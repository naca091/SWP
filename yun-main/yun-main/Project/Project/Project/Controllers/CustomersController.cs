using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;
using Project.Data;
using Project.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Net.Mail;
using System.Net;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Project.Controllers
{
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomersController(ApplicationDbContext context)
        {
            _context = context;
        }

        //GET Index
        public async Task<IActionResult> Index(string searchString, int? pageNumber)
        {
            var customers = from c in _context.Customers
                            select c;

            if (!String.IsNullOrEmpty(searchString))
            {
                customers = customers.Where(c => c.Name.Contains(searchString));
            }

            int pageSize = 5; // Change this number to the desired number of items per page
            return View(await PaginatedList<Customer>.CreateAsync(customers.AsNoTracking(), pageNumber ?? 1, pageSize));
        }


        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(m => m.CustomerID == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // GET: Customers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Address,Email,Phone")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(customer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CustomerID,Name,Address,Email,Phone")] Customer customer)
        {
            if (id != customer.CustomerID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Tìm kiếm Customer hiện tại trong database
                    var currentCustomer = _context.Customers.AsNoTracking().FirstOrDefault(c => c.CustomerID == id);
                    if (currentCustomer == null)
                    {
                        return NotFound();
                    }

                    // Cập nhật thông tin Customer mới
                    _context.Entry(customer).State = EntityState.Modified;

                    // Lấy danh sách Note liên quan đến Customer hiện tại
                    var relatedNotes = _context.Notes.Where(n => n.Phone == currentCustomer.Phone).ToList();

                    // Cập nhật thông tin Customer trong các Note liên quan
                    foreach (var note in relatedNotes)
                    {
                        note.Customer = currentCustomer.Name;
                        note.Phone = currentCustomer.Phone;
                        note.AddressCustomer = currentCustomer.Address;
                        _context.Entry(note).State = EntityState.Modified;
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(customer.CustomerID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }


        // GET: Customers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(m => m.CustomerID == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.CustomerID == id);
        }

        [HttpGet]
        public JsonResult IsPhoneExist(string Phone)
        {
            return Json(_context.Customers.Any(c => c.Phone == Phone));
        }

        public ActionResult SendMail(int id)
        {
            // Get the customer with the given id
            var customer = _context.Customers.Find(id);

            if (customer == null)
            {
                return NotFound();
            }

            // Pass the customer to the view
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SendMail(int id, string Subject, string Body)
        {
            // Get the customer with the given id
            var customer = _context.Customers.Find(id);

            if (customer == null)
            {
                return NotFound();
            }

            // Set up the email sender
            var fromAddress = new MailAddress("dinhquoctien1980@gmail.com", "From Name");
            var toAddress = new MailAddress(customer.Email, "To Name");
            const string fromPassword = ""; // Replace with your email password
            string subject = Subject;
            string body = Body;

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                smtp.Send(message);
            }

            return RedirectToAction("Index");
        }

        public IActionResult Search(string searchText)
        {
            // Logic để tìm kiếm khách hàng dựa trên searchText
            var customers = _context.Customers.Where(c => c.Name.Contains(searchText)).ToList();
            return View("Index", customers);
        }

        public async Task UpdateCustomerTotal(string phone)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == phone);
            if (customer != null)
            {
                var notes = _context.Notes.Where(n => n.Phone == customer.Phone && n.Status == 3);
                customer.Total = notes.Sum(n => n.Total);
                _context.Entry(customer).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
        }

        [HttpGet]
        public IActionResult ProductCount()
        {
            var lowQuantityProductCount = _context.Products.Count(p => p.Quantity < 10);
            return Json(lowQuantityProductCount);
        }
        public async Task<IActionResult> Download()
        {
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Customers");
                AddValueWithBorder(worksheet, 1, 1, "Customer ID");
                AddValueWithBorder(worksheet, 1, 2, "Name");
                AddValueWithBorder(worksheet, 1, 3, "Address");
                AddValueWithBorder(worksheet, 1, 4, "Email");
                AddValueWithBorder(worksheet, 1, 5, "Phone");
                AddValueWithBorder(worksheet, 1, 6, "Rank"); // Change Total to Rank

                var customers = await _context.Customers.ToListAsync();
                for (int i = 0; i < customers.Count; i++)
                {
                    AddValueWithBorder(worksheet, i + 2, 1, customers[i].CustomerID);
                    AddValueWithBorder(worksheet, i + 2, 2, customers[i].Name);
                    AddValueWithBorder(worksheet, i + 2, 3, customers[i].Address);
                    AddValueWithBorder(worksheet, i + 2, 4, customers[i].Email);
                    AddValueWithBorder(worksheet, i + 2, 5, customers[i].Phone);
                    AddValueWithBorder(worksheet, i + 2, 6, GetRank(customers[i].Total)); // Use GetRank function to convert Total to Rank
                }

                return File(package.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Customers.xlsx");
            }
        }

        // Function to convert Total to Rank
        public string GetRank(decimal total)
        {
            if (total > 1000000)
                return "Diamond";
            else if (total > 500000)
                return "Platinum";
            else if (total > 200000)
                return "Gold";
            else if (total > 100000)
                return "Silver";
            else
                return "Bronze";
        }
        private void AddValueWithBorder(ExcelWorksheet worksheet, int row, int column, object value)
        {
            var cell = worksheet.Cells[row, column];
            cell.Value = value;
            cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }
    }
}

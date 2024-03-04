using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Project.Data;
using Project.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Controllers
{
    public class NotesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public NotesController(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }
        // GET: Notes

        public async Task<IActionResult> Index(string sortOrder, int? pageNumber, string searchString, DateTime? fromDate, DateTime? toDate)
        {

            IQueryable<Note> notes = _context.Notes
                .Include(n => n.NoteProducts)
                .ThenInclude(np => np.Product);

            switch (sortOrder)
            {
                case "newest":
                    notes = notes.OrderByDescending(s => s.CreatedDate);
                    break;
                case "oldest":
                    notes = notes.OrderBy(s => s.CreatedDate);
                    break;
            }
            if (!String.IsNullOrEmpty(searchString))
            {
                notes = notes.Where(s => s.Phone.Contains(searchString));
            }
            if (fromDate.HasValue && toDate.HasValue)
            {
                notes = notes.Where(s => s.CreatedDate >= fromDate && s.CreatedDate <= toDate);
            }
            if (fromDate.HasValue && toDate.HasValue)
            {
                notes = notes.Where(s => s.CreatedDate >= fromDate && s.CreatedDate <= toDate);
                TempData["FromDate"] = fromDate.Value.ToString("yyyy-MM-dd");
                TempData["ToDate"] = toDate.Value.ToString("yyyy-MM-dd");
            }

            int pageSize = 10;
            return View(await PaginatedList<Note>.CreateAsync(notes.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        //get create 
        public async Task<IActionResult> Create()
        {
            // Chỉ lấy những sản phẩm có trong kho
            var inStockProducts = _context.Products.Where(p => p.InStock).ToList();
            ViewBag.Products = inStockProducts;

            // Lấy danh sách khách hàng và số điện thoại
            ViewBag.Customers = _context.Customers.ToList();
            ViewBag.Phones = _context.Customers.Select(c => c.Phone).ToList();

            var currentUser = await _userManager.GetUserAsync(User);
            var model = new NoteViewModel
            {
                UserName = currentUser.UserName,
                Products = new List<NoteProductViewModel> { new NoteProductViewModel() } // Initialize the Products list with one element
            };

            return View(model);
        }


        // POST: Notes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create(NoteViewModel model)
        {
            // Kiểm tra sự tồn tại của sản phẩm trong cơ sở dữ liệu
            foreach (var productViewModel in model.Products)
            {
                var productInDb = _context.Products.FirstOrDefault(p => p.ProductID == productViewModel.ProductID);
                if (productInDb == null)
                {
                    ModelState.AddModelError("", "Invalid product selection.");
                    return View(model);
                }
            }

            if (ModelState.IsValid)
            {
                var phoneAsInt = int.Parse(model.Phone);
                var customer = _context.Customers.FirstOrDefault(c => c.CustomerID == phoneAsInt);
                if (customer == null)
                {
                    ModelState.AddModelError("", "Invalid customer selection.");
                    return View(model);
                }


                // Map NoteViewModel to Note entity
                var note = new Note
                {
                    NoteCode = model.NoteCode,
                    CreateName = model.UserName,
                    Customer = customer.Name,
                    Phone = customer.Phone, // Lấy số điện thoại từ Customer
                    AddressCustomer = customer.Address,
                    Reason = model.Reason,
                    Status = 1,
                    CreatedDate = DateTime.Now,
                    Total = model.Products.Sum(p => p.StockOut * _context.Products.FirstOrDefault(prod => prod.ProductID == p.ProductID).Price)

                };

                _context.Notes.Add(note);
                await _context.SaveChangesAsync();
                if (note.Status == 3)
                {
                    // Tìm Customer dựa trên Phone của Note
                    customer = _context.Customers.FirstOrDefault(c => c.Phone == note.Phone);
                    if (customer != null)
                    {
                        // Cập nhật Total của Customer
                        customer.Total += note.Total;
                        _context.Customers.Update(customer);
                        await _context.SaveChangesAsync();
                    }
                }


                foreach (var productViewModel in model.Products)
                {
                    var noteProduct = new NoteProduct
                    {
                        NoteId = note.NoteId, // Set NoteId from the saved note
                        ProductID = productViewModel.ProductID,
                        StockOut = productViewModel.StockOut
                        // Optionally, you can set other properties of NoteProduct here
                    };

                    _context.NoteProducts.Add(noteProduct);
                }

                foreach (var product in model.Products)
                {
                    var productInDb = _context.Products.FirstOrDefault(p => p.ProductID == product.ProductID);
                    if (productInDb != null)
                    {
                        productInDb.Quantity -= product.StockOut; // Giảm số lượng sản phẩm bằng số lượng nhập
                    }
                }

                await _context.SaveChangesAsync(); // Save changes to save NoteProducts

                return RedirectToAction(nameof(Index));
            }

            // If ModelState is not valid, return to the create view with errors
            return View(model);
        }

        // GET: Notes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var note = await _context.Notes.FindAsync(id);
            if (note == null)
            {
                return NotFound();
            }
            return View(note);
        }

        // POST: Notes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("NoteId,NoteCode,CreateName,Customer,AddressCustomer,Reason,Status")] Note note)
        {
            if (id != note.NoteId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(note);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NoteExists(note.NoteId))
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
            return View(note);
        }

        // GET: Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var note = await _context.Notes
                .Include(n => n.NoteProducts)
                .ThenInclude(np => np.Product) // Include Product information
                .FirstOrDefaultAsync(m => m.NoteId == id);

            if (note == null)
            {
                return NotFound();
            }

            return View(note);
        }



        // GET: Notes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var note = await _context.Notes
                .FirstOrDefaultAsync(m => m.NoteId == id);
            if (note == null)
            {
                return NotFound();
            }

            return View(note);
        }

        // POST: Notes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var note = await _context.Notes.FindAsync(id);
            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatusAjax(int id, int newStatus)
        {
            var note = await _context.Notes.FindAsync(id);

            if (note == null)
            {
                return NotFound();
            }

            note.UpdateStatus(newStatus);
            _context.Entry(note).State = EntityState.Modified;

            // Nếu Status mới bằng 3, cập nhật Total của Customer
            if (newStatus == 3)
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == note.Phone);
                if (customer != null)
                {
                    customer.Total += note.Total;
                    _context.Customers.Update(customer);
                }
            }

            await _context.SaveChangesAsync();

            return Ok();
        }


        private bool NoteExists(int id)
        {
            return _context.Notes.Any(e => e.NoteId == id);
        }
        public IActionResult GetNewNoteCount()
        {
            int newNoteCount = _context.Notes.Count(n => n.Status == 2);
            return Json(newNoteCount);
        }

        public IActionResult CheckNoteStatus()
        {
            bool hasNoteStatus34 = _context.Notes.Any(n => n.Status == 3 || n.Status == 4);
            return Json(hasNoteStatus34);
        }

        public IActionResult DownloadNoteDetails(int id)
        {
            var note = _context.Notes.Include(n => n.NoteProducts).ThenInclude(np => np.Product).FirstOrDefault(n => n.NoteId == id);
            if (note == null)
            {
                return NotFound();
            }
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var worksheet = package.Workbook.Worksheets.Add("Note Details");

                // Add Note details
                AddValueWithBorder(worksheet, 1, 3, "Note Delivery Goods Information");
                AddValueWithBorder(worksheet, 2, 1, "Note Code:");
                AddValueWithBorder(worksheet, 2, 2, note.NoteCode);

                AddValueWithBorder(worksheet, 3, 1, "Created's Name:");
                AddValueWithBorder(worksheet, 3, 2, note.CreateName);

                AddValueWithBorder(worksheet, 4, 1, "Customer:");
                AddValueWithBorder(worksheet, 4, 2, note.Customer);

                AddValueWithBorder(worksheet, 5, 1, "Customer's Address:");
                AddValueWithBorder(worksheet, 5, 2, note.AddressCustomer);

                AddValueWithBorder(worksheet, 6, 1, "Reason:");
                AddValueWithBorder(worksheet, 6, 2, note.Reason);

                AddValueWithBorder(worksheet, 7, 1, "Date Created");
                AddValueWithBorder(worksheet, 7, 2, note.CreatedDate.ToString());
                AddValueWithBorder(worksheet, 8, 1, "Phone Number:");
                AddValueWithBorder(worksheet, 8, 2, note.Phone);
                AddValueWithBorder(worksheet, 9, 1, "Status:");
                AddValueWithBorder(worksheet, 9, 2, ConvertStatus(note.Status));
                AddValueWithBorder(worksheet, 11, 3, "Product to Export");

                // Add header for Products
                AddValueWithBorder(worksheet, 12, 1, "Product Name");
                AddValueWithBorder(worksheet, 12, 2, "Product Code");
                AddValueWithBorder(worksheet, 12, 3, "StockOut");
                AddValueWithBorder(worksheet, 12, 4, "Price");
                AddValueWithBorder(worksheet, 12, 5, "Total");

                // Add data for Products
                int row = 12;
                foreach (var product in note.NoteProducts)
                {
                    AddValueWithBorder(worksheet, row, 1, product.Product.ProductName);
                    AddValueWithBorder(worksheet, row, 2, product.Product.ProductCode);
                    AddValueWithBorder(worksheet, row, 3, product.StockOut);
                    AddValueWithBorder(worksheet, row, 4, product.Product.Price);
                    AddValueWithBorder(worksheet, row, 5, product.StockOut * product.Product.Price);
                    row++;
                }

                // Add total of Note
                AddValueWithBorder(worksheet, row, 4, "Total of Note:");
                AddValueWithBorder(worksheet, row, 5, note.NoteProducts.Sum(p => p.StockOut * p.Product.Price));

                // Save the Excel package to a MemoryStream
                var stream = new MemoryStream();
                package.SaveAs(stream);

                // Return the Excel file as a FileContentResult
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Note Code: " + note.NoteCode + ".xlsx");
            }
        }
        public IActionResult DownloadSearchResults()
        {
            DateTime fromDate = DateTime.Parse(TempData["FromDate"].ToString());
            DateTime toDate = DateTime.Parse(TempData["ToDate"].ToString());

            IQueryable<Note> notes = _context.Notes.Include(n => n.NoteProducts).ThenInclude(np => np.Product);

            notes = notes.Where(s => s.CreatedDate.Date >= fromDate.Date && s.CreatedDate.Date <= toDate.Date);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var worksheet = package.Workbook.Worksheets.Add("Search Results");

                // Add header
                AddValueWithBorder(worksheet, 1, 1, "Note Code");
                AddValueWithBorder(worksheet, 1, 2, "Created's Name");
                AddValueWithBorder(worksheet, 1, 3, "Customer");
                AddValueWithBorder(worksheet, 1, 4, "Customer's Address");
                AddValueWithBorder(worksheet, 1, 5, "Reason");
                AddValueWithBorder(worksheet, 1, 6, "Date Created");
                AddValueWithBorder(worksheet, 1, 7, "Products and StockOut"); // New column
                AddValueWithBorder(worksheet, 1, 8, "Total");
                AddValueWithBorder(worksheet, 1, 9, "Status");

                // Add data
                int row = 2;
                foreach (var note in notes)
                {
                    AddValueWithBorder(worksheet, row, 1, note.NoteCode);
                    AddValueWithBorder(worksheet, row, 2, note.CreateName);
                    AddValueWithBorder(worksheet, row, 3, note.Customer);
                    AddValueWithBorder(worksheet, row, 4, note.AddressCustomer);
                    AddValueWithBorder(worksheet, row, 5, note.Reason);
                    AddValueWithBorder(worksheet, row, 6, note.CreatedDate.ToString());

                    // Create a string containing all products and their StockOut, separated by newlines
                    var productsAndStockOut = string.Join("\n", note.NoteProducts.Select(np => np.Product.ProductName + ": " + np.StockOut));
                    AddValueWithBorder(worksheet, row, 7, productsAndStockOut);

                    AddValueWithBorder(worksheet, row, 8, note.NoteProducts.Sum(p => p.StockOut * p.Product.Price));
                    AddValueWithBorder(worksheet, row, 9, ConvertStatus(note.Status));

                    row++;
                }

                // Save the Excel package to a MemoryStream
                var stream = new MemoryStream();
                package.SaveAs(stream);

                // Return the Excel file as a FileContentResult
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SearchResults.xlsx");
            }
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
        private string ConvertStatus(int status)
        {
            switch (status)
            {
                case 1:
                case 2:
                    return "Waiting..";
                case 3:
                    return "Approved";
                case 4:
                    return "Disapproved";
                default:
                    return "Unknown status";
            }
        }
        public ActionResult PrintNoteDetails(int id)
        {
            var note = _context.Notes.Find(id);
            if (note == null)
            {
                return NotFound();
            }

            return PartialView("_NoteDetails", note);
        }

        [HttpGet]
        public IActionResult GetCustomerDetails(int customerId)
        {
            var customer = _context.Customers.FirstOrDefault(c => c.CustomerID == customerId);
            if (customer != null)
            {
                return Json(new { customer = new { name = customer.Name, address = customer.Address } });
            }
            return Json(new { customer = (Customer)null });
        }

        public async Task<IActionResult> UpdateCustomerTotal(int noteId)
        {
            // Tìm Note dựa trên noteId
            var note = await _context.Notes.FindAsync(noteId);
            if (note == null)
            {
                return NotFound();
            }

            // Kiểm tra xem Status của Note có bằng 3 hay không
            if (note.Status == 3)
            {
                // Tìm Customer dựa trên Phone của Note
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == note.Phone);
                if (customer != null)
                {
                    // Cập nhật Total của Customer
                    customer.Total += note.Total;
                    _context.Customers.Update(customer);
                    await _context.SaveChangesAsync();
                }
            }

            // Quay lại trang quản lý Customer hoặc trang khác tùy thuộc vào yêu cầu của bạn
            return RedirectToAction(nameof(Index));
        }

    }
}

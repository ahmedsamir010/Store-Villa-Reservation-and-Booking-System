using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using Store.Application.Common;
using Store.Application.Common.Interfaces;
using Store.Domain.Entities;
using Store.Infrastructre.Data;

namespace Villa_PL.Controllers
{
    public class VillaController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IToastNotification _toastNotification;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public VillaController(IUnitOfWork unitOfWork, IToastNotification toastNotification , IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _toastNotification = toastNotification;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Index()
        {
            IEnumerable<Villa> villas = await _unitOfWork.Repository<Villa>().GetAllAsync();
            return View(villas);
        }
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Villa villa)
        {
            if (ModelState.IsValid)
            {
                if (villa.Image != null && villa.Image.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(villa.Image.FileName);
                    string imagesPath = Path.Combine(_webHostEnvironment.WebRootPath, "Images", "VillaImage");
                    string filePath = Path.Combine(imagesPath, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await villa.Image.CopyToAsync(fileStream);
                    }

                    villa.ImageUrl = "/Images/VillaImage/" + fileName;
                }
                else
                {
                    villa.ImageUrl = "https://placehold.co/400*400";
                }

                await _unitOfWork.Repository<Villa>().CreateAsync(villa);
                await _unitOfWork.CompleteAsync();
                _toastNotification.AddSuccessToastMessage("Villa Created Successfully");

                return RedirectToAction(nameof(Index));
            }

            return View(villa);
        }


        public async Task<IActionResult> Edit(int villaId)
        {
            Villa? villa = await _unitOfWork.Repository<Villa>().GetByIdAsync(v => v.Id == villaId);

            if (villa is not null)
            {
                return PartialView("EditVillaPartialView", villa);
            }
            _toastNotification.AddErrorToastMessage("Error , Please Try Again");

            return RedirectToAction("Error", "Home");
        }
        [HttpPost]
        public async Task<IActionResult> SaveEdit(Villa villa)
        {
            if (villa != null)
            {
                Villa existingVilla = await _unitOfWork.Repository<Villa>().GetByIdAsync(v => v.Id == villa.Id);

                if (villa.Image != null && villa.Image.Length > 0) // Check if an image is uploaded
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(villa.Image.FileName);
                    string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "Images", "VillaImage");

                    // Create the directory if it doesn't exist
                    Directory.CreateDirectory(imagePath);

                    if (!string.IsNullOrEmpty(existingVilla.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingVilla.ImageUrl.TrimStart('/'));

                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    string newImagePath = Path.Combine("Images", "VillaImage", fileName);
                    string fullImagePath = Path.Combine(_webHostEnvironment.WebRootPath, newImagePath);

                    using var fileStream = new FileStream(fullImagePath, FileMode.Create);
                    await villa.Image.CopyToAsync(fileStream);

                    existingVilla.ImageUrl = $"/{newImagePath}"; // Update existingVilla.ImageUrl
                }
                else
                {
                    existingVilla.ImageUrl = existingVilla.ImageUrl; // No change in ImageUrl
                }

                existingVilla.Name = villa.Name;
                existingVilla.Description = villa.Description;

                await _unitOfWork.Repository<Villa>().UpdateAsync(existingVilla);
                await _unitOfWork.CompleteAsync();
                _toastNotification.AddSuccessToastMessage("Villa Updated Successfully");

                return RedirectToAction(nameof(Index));
            }

            _toastNotification.AddErrorToastMessage("Invalid Villa Data");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int villaId)
        {
            var villa = await _unitOfWork.Repository<Villa>().GetByIdAsync(v => v.Id == villaId);

            if (villa is not null)
            {
                return PartialView("DetailsVillaPartialView", villa);
            }
            _toastNotification.AddErrorToastMessage("Error , Please Try Again");

            return RedirectToAction("Error", "Home");
        }
        public async Task<IActionResult> Delete(int villaId)
        {
            var villa = await _unitOfWork.Repository<Villa>().GetByIdAsync(v=>v.Id==villaId);

            if (villa is not null)
            {
                return PartialView("DeleteVillaPartialView", villa);
            }
            _toastNotification.AddErrorToastMessage("Error , Please Try Again");
            return RedirectToAction("Error", "Home");
        }
        [HttpPost]
        public async Task<IActionResult> Delete(Villa villa)
        {
            Villa obj = await _unitOfWork.Repository<Villa>().GetByIdAsync(v => v.Id == villa.Id);

            if (obj != null)
            {
                if (!string.IsNullOrEmpty(obj.ImageUrl))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, obj.ImageUrl.TrimStart('/'));

                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                    else
                    {
                        Console.WriteLine($"Image file not found: {imagePath}");
                    }
                }
                await _unitOfWork.Repository<Villa>().DeleteAsync(obj);
                await _unitOfWork.CompleteAsync();
                _toastNotification.AddSuccessToastMessage("Villa Deleted Successfully");
                return RedirectToAction(nameof(Index));
            }

            _toastNotification.AddErrorToastMessage("Villa not found");
            return RedirectToAction(nameof(Index));
        }



    }
}

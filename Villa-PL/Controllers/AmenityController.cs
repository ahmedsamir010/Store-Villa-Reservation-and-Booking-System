using Microsoft.AspNetCore.Mvc;
using NToastNotify;
using Store.Application.Common;
using Store.Domain.Entities;
using Villa_PL.ViewModel;

namespace Villa_PL.Controllers
{
    public class AmenityController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IToastNotification _toastNotification;

        public AmenityController(IUnitOfWork unitOfWork , IToastNotification toastNotification)
        {
            _unitOfWork = unitOfWork;
            _toastNotification = toastNotification;
        }
        public async Task<IActionResult> Index()
        {
            var amenities = await _unitOfWork.Repository<Amenity>().GetAllAsync(includeProperties:v=>v.villa);
            var villas = await _unitOfWork.Repository<Amenity>().GetAllListVillaAsync();

            var viewModel = new AmenityVM
            {
                amenities = amenities.AsEnumerable(), 
                villaList = villas.ToList(),
            };

            return View(viewModel); 
        }


        public async Task<IActionResult> Create()
        {

            var villas = await _unitOfWork.Repository<Amenity>().GetAllListVillaAsync();

            var viewModel = new AmenityVM
            {
                villaList = villas.ToList(),
            };
            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> Create(AmenityVM amenityVM)
        {
            if (amenityVM?.amenity == null)
            {
                _toastNotification.AddErrorToastMessage("Invalid data provided for creating a Amenity Number.");
                return RedirectToAction("Error", "Home");
            }

            var amenityNumber = amenityVM.amenity;

            if (await _unitOfWork.Repository<Amenity>().AnyAsync(item => item.Id == amenityNumber.Id))
            {
                _toastNotification.AddErrorToastMessage($"Amenity Number {amenityNumber.VillaId} already exists. Please choose a different Amenity Number.");
                return View(amenityVM);
            }
            amenityVM.villaList = await _unitOfWork.Repository<Amenity>().GetAllListVillaAsync();

            await _unitOfWork.Repository<Amenity>().CreateAsync(amenityNumber);
            await _unitOfWork.CompleteAsync();

            _toastNotification.AddSuccessToastMessage($"Amenity Name {amenityNumber.Name} created successfully.");

            return RedirectToAction("Index");
        }


        public async Task<IActionResult> Details(int AmenityNumber)
        {
            var amenityNumber = await _unitOfWork.Repository<Amenity>().GetByIdAsync(a => a.Id == AmenityNumber);
            if (amenityNumber == null)
            {
                _toastNotification.AddErrorToastMessage("Error , Please Try Again");
                return RedirectToAction("Error", "Home");
            }
            else
            {
                return PartialView("DetailsVillaNumberPartialView", amenityNumber);
            }
        }

        public async Task<IActionResult> DeleteById(int amenityId)
        {
            var amenity = _unitOfWork.Repository<Amenity>().DeleteByIdAsync(amenityId);

            if (amenity != null)
            {
                return PartialView("DeleteVillaNumberPartialView", amenity);
            }

            _toastNotification.AddErrorToastMessage("Error, Please Try Again");
            return RedirectToAction("Error", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(AmenityVM amenityVM)
        {
            var amenityNum = amenityVM.amenity?.Id;
            if (amenityNum == null)
            {   
                _toastNotification.AddErrorToastMessage("Invalid data provided for deletion.");
                return RedirectToAction("Error", "Home");
            }

            var amenity = await _unitOfWork.Repository<Amenity>().GetByIdAsync(a =>a.Id == amenityVM.amenity.Id);
            
            if (amenity != null)
            {
                await _unitOfWork.Repository<Amenity>().DeleteAsync(amenity);
                await _unitOfWork.CompleteAsync();
                _toastNotification.AddSuccessToastMessage("Amenity Deleted Successfully");
                return RedirectToAction("Index");
            }

            _toastNotification.AddErrorToastMessage("Amenity is not found or already deleted.");
            return RedirectToAction("Error", "Home");
        }


        public async Task<IActionResult> Edit(int AmenityId)
        {
            try
            {
                var amenity = await _unitOfWork.Repository<Amenity>().GetByIdAsync(a => a.Id == AmenityId);

                if (amenity == null)
                {
                    _toastNotification.AddErrorToastMessage("Error, Amenity not found");
                    return RedirectToAction("Error", "Home");
                }

                var villaList = await _unitOfWork.Repository<Amenity>().GetAllListVillaAsync();

                var viewModel = new AmenityVM
                {
                    amenity = amenity,
                    villaList = villaList.ToList(),
                };

                return PartialView("EditAmenityPartialView", viewModel);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine("An error occurred while loading the Amenity for editing: " + ex.Message);
                _toastNotification.AddErrorToastMessage("An error occurred while loading the Amenity for editing. Please try again.");
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveEdit(AmenityVM amenityVM)
        {

                var existingAmenity = await _unitOfWork.Repository<Amenity>().GetByIdAsync(a => a.Id == amenityVM.amenity.Id);

                if (existingAmenity == null)
                {
                    _toastNotification.AddErrorToastMessage("Error, Amenity not found");
                    return RedirectToAction("Index", "Home");
                }

                bool isDuplicateAmenityName = await _unitOfWork.Repository<Amenity>().AnyAsync(a => a.Id != existingAmenity.Id && a.Name == amenityVM.amenity.Name);

                if (isDuplicateAmenityName)
                {
                    _toastNotification.AddWarningToastMessage("Amenity Name already exists. Please enter a different Amenity Name.");
                }
                else
                {
                    // Update only the necessary properties
                    existingAmenity.VillaId = amenityVM.amenity.VillaId;
                    existingAmenity.Name = amenityVM.amenity.Name;
                    existingAmenity.Description = amenityVM.amenity.Description;
                    // Update other properties as needed

                    await _unitOfWork.CompleteAsync();
                    _toastNotification.AddSuccessToastMessage("Amenity Updated Successfully");
                }

                amenityVM.villaList = await _unitOfWork.Repository<Amenity>().GetAllListVillaAsync();
                return RedirectToAction("Index");
            }
    }
}

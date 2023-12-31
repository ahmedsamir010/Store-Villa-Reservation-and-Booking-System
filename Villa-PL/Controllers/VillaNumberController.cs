using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using Store.Application.Common;
using Store.Domain.Entities;
using Store.Infrastructre.Data;
using Villa_PL.ViewModel;

namespace Villa_PL.Controllers
{
    public class VillaNumberController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IToastNotification _toastNotification;

        public VillaNumberController(IUnitOfWork unitOfWork,IToastNotification toastNotification)
        {
            _unitOfWork = unitOfWork;
            _toastNotification = toastNotification;
        }

        public async Task<IActionResult> Index()
        {
            var villaNumbers = await _unitOfWork.Repository<VillaNumber>().GetAllAsync(includeProperties: v => v.Villa);
            var villas = await _unitOfWork.Repository<VillaNumber>().GetAllListVillaAsync();
            var viewModel = new VillaNumberVM
            {
                VillaNumbers = villaNumbers,  
                villaList=villas.ToList(),
            };
            return View(viewModel);
        }
        public async Task<IActionResult> Create()
        {
         
            var villas = await _unitOfWork.Repository<VillaNumber>().GetAllListVillaAsync();

            var viewModel = new VillaNumberVM
            {
                villaList = villas.ToList(),
            };
            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> Create(VillaNumberVM villaNumberVM)
        {
            if (villaNumberVM?.villaNumber == null)
            {
                _toastNotification.AddErrorToastMessage("Invalid data provided for creating a VillaNumber.");
                return RedirectToAction("Error", "Home");
            }

            var villaNumber = villaNumberVM.villaNumber;

            if (await _unitOfWork.Repository<VillaNumber>().AnyAsync(item => item.Villa_Number == villaNumber.Villa_Number))
             {
                 _toastNotification.AddErrorToastMessage($"VillaNumber {villaNumber.Villa_Number} already exists. Please choose a different VillaNumber.");

            villaNumberVM.villaList = await _unitOfWork.Repository<VillaNumber>().GetAllListVillaAsync();
                 return View(villaNumberVM);
             }

            villaNumberVM.villaList = await _unitOfWork.Repository<VillaNumber>().GetAllListVillaAsync(); 

            await _unitOfWork.Repository<VillaNumber>().CreateAsync(villaNumber);
            await _unitOfWork.CompleteAsync();

            _toastNotification.AddSuccessToastMessage($"VillaNumber {villaNumber.Villa_Number} created successfully.");

            return RedirectToAction("Index");
        }





        public async Task<IActionResult> Details(int villaNumberId)
        {
            var villaId = await _unitOfWork.Repository<VillaNumber>().GetByIdAsync(v => v.Villa_Number == villaNumberId);
            if (villaId == null)
            {
                _toastNotification.AddErrorToastMessage("Error , Please Try Again");
                return RedirectToAction("Error", "Home");
            }
            else
            {
                return PartialView("DetailsVillaNumberPartialView", villaId);
            }
        }
        public async Task<IActionResult> Edit(int villaId)
        {
            try
            {
                var villaNumber = await _unitOfWork.Repository<VillaNumber>().GetByIdAsync(v => v.VillaId == villaId);

                if (villaNumber == null)
                {
                    _toastNotification.AddErrorToastMessage("Error, Villa not found");
                    return RedirectToAction("Error", "Home");
                }

                var villaSelectList = await _unitOfWork.Repository<VillaNumber>().GetAllListVillaAsync();

                VillaNumberVM villaNumberVM = new VillaNumberVM
                {
                    villaNumber = villaNumber,
                    villaList = villaSelectList,
                    SelectedVillaId = villaNumber.VillaId // Assuming you want to set the selected villa in the dropdown
                };

                return PartialView("EditVillaNumberPartialView", villaNumberVM);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine("An error occurred while retrieving the Villa Number for editing: " + ex.Message);
                _toastNotification.AddErrorToastMessage("An error occurred while retrieving the Villa Number for editing. Please try again.");
                return RedirectToAction("Error", "Home");
            }
        }


        [HttpPost]
        public async Task<IActionResult> SaveEdit(VillaNumberVM villaNumberVM)
        {
                var existingVilla = await _unitOfWork.Repository<VillaNumber>().GetByIdAsync(v => v.VillaId == villaNumberVM.villaNumber.VillaId);

                if (existingVilla == null)
                {
                    _toastNotification.AddErrorToastMessage("Error, Villa not found");
                    return RedirectToAction("Error", "Home");
                }

                bool isDuplicateVillaNumber = await _unitOfWork.Repository<VillaNumber>().AnyAsync(v => v.Id != existingVilla.Id && v.Villa_Number == villaNumberVM.villaNumber.Villa_Number);

                if (isDuplicateVillaNumber)
                {
                    _toastNotification.AddWarningToastMessage("Villa Number already exists. Please enter a different Villa Number.");
                }
                else
                {
                    // Update only the necessary properties
                    existingVilla.Villa_Number = villaNumberVM.villaNumber.Villa_Number;
                    existingVilla.SpecialDetails = villaNumberVM.villaNumber.SpecialDetails;
                    // Update other properties as needed

                    await _unitOfWork.CompleteAsync();
                    _toastNotification.AddSuccessToastMessage("Villa Number Updated Successfully");
                }

                // Reload villaList in case ModelState becomes invalid after the update
                villaNumberVM.villaList = await _unitOfWork.Repository<VillaNumber>().GetAllListVillaAsync();
                return RedirectToAction("Index");
        }




        public async Task<IActionResult> DeleteById(int villaId)
        {
            var villa = _unitOfWork.Repository<VillaNumber>().DeleteByIdAsync(villaId);

            if (villa != null)
            {
                return PartialView("DeleteVillaNumberPartialView", villa);
            }

            _toastNotification.AddErrorToastMessage("Error, Please Try Again");
            return RedirectToAction("Error", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(VillaNumberVM villaNumberVM)
        {
            if (villaNumberVM?.villaNumber == null)
            {
                _toastNotification.AddErrorToastMessage("Invalid data provided for deletion.");
                return RedirectToAction("Error", "Home");
            }

            VillaNumber villaNumber = await _unitOfWork.Repository<VillaNumber>().GetByIdAsync(v => v.Villa_Number == villaNumberVM.villaNumber.Villa_Number);

            if (villaNumber != null)
            {

                await _unitOfWork.Repository<VillaNumber>().DeleteAsync(villaNumber);
              await _unitOfWork.CompleteAsync();
                _toastNotification.AddSuccessToastMessage("Villa Number Deleted Successfully");
                return RedirectToAction("Index");
            }

            _toastNotification.AddErrorToastMessage("Villa Number not found or already deleted.");
            return RedirectToAction("Error", "Home");
        }


    }
}

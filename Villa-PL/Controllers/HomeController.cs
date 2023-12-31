using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using NToastNotify;
using Store.Application.Common;
using Store.Application.Common.Utility;
using Store.Domain.Entities;
using Syncfusion.Presentation;
using System.Diagnostics;
using Villa_PL.Models;
using Villa_PL.ViewModel;

namespace Villa_PL.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HomeController(IUnitOfWork unitOfWork,IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            HomeVM homeVM = new()
            {
                villaList = await _unitOfWork.Repository<Villa>().GetAllAsync(includeProperties:v=>v.VillaAmenity),
                Nights = 1,
                CheckInDate = DateOnly.FromDateTime(DateTime.Now),
            };
            return View(homeVM);
        }
        [HttpPost]
        public async Task<IActionResult> GetVillaListByDate(int nights, DateOnly checkInDate)
        {
            await Task.Delay(2000);

            var villaListTask = await _unitOfWork.Repository<Villa>().GetAllAsync(includeProperties: v=>v.VillaAmenity);
            var villaNumbersListTask = await _unitOfWork.Repository<VillaNumber>().GetAllAsync();
            var bookedVillasTask =await _unitOfWork.Repository<Booking>().GetAllAsync(u => u.Status == Const.BookingStatus.Completed);

            foreach (var villa in villaListTask)
            {
                int roomAvailable = Const.CalculateAvailableRoomsForNights(villa.Id, villaNumbersListTask, checkInDate, nights, bookedVillasTask);
                villa.IsAvailable=roomAvailable > 0 ?true :false;
            }

            HomeVM homeVM = new()
            {
                Nights = nights,
                villaList = villaListTask,
                CheckInDate = checkInDate
            };

            return PartialView("_VillaList", homeVM);
        }
        [HttpPost]
        public async Task<IActionResult> GeneratePPt(int id)
        {
            var villa = await _unitOfWork.Repository<Villa>().GetByIdAsync(v => v.Id == id, includeProperties: a => a.VillaAmenity);


            string basePath = _webHostEnvironment.WebRootPath;
            string filePath = basePath + @"/exports/ExportVillaDetails.pptx";


            using IPresentation presentation = Presentation.Open(filePath);

            ISlide slide = presentation.Slides[0];


            IShape? shape = slide.Shapes.FirstOrDefault(u => u.ShapeName == "txtVillaName") as IShape;
            if (shape is not null)
            {
                shape.TextBody.Text = villa.Name;
            }

            shape = slide.Shapes.FirstOrDefault(u => u.ShapeName == "txtVillaDescription") as IShape;
            if (shape is not null)
            {
                shape.TextBody.Text = villa.Description;
            }


            shape = slide.Shapes.FirstOrDefault(u => u.ShapeName == "txtOccupancy") as IShape;
            if (shape is not null)
            {
                shape.TextBody.Text = string.Format("Max Occupancy : {0} adults", villa.Occupancy);
            }
            shape = slide.Shapes.FirstOrDefault(u => u.ShapeName == "txtVillaSize") as IShape;
            if (shape is not null)
            {
                shape.TextBody.Text = string.Format("Villa Size: {0} sqft", villa.Sqft);
            }
            shape = slide.Shapes.FirstOrDefault(u => u.ShapeName == "txtPricePerNight") as IShape;
            if (shape is not null)
            {
                shape.TextBody.Text = string.Format("AED {0}/night", villa.Price.ToString("C"));
            }


            shape = slide.Shapes.FirstOrDefault(u => u.ShapeName == "txtVillaAmenitiesHeading") as IShape;
            if (shape is not null)
            {
                List<string> listItems = villa.VillaAmenity.Select(x => x.Name).ToList();

                shape.TextBody.Text = "";

                foreach (var item in listItems)
                {
                    IParagraph paragraph = shape.TextBody.AddParagraph();
                    ITextPart textPart = paragraph.AddTextPart(item);

                    paragraph.ListFormat.Type = ListType.Bulleted;
                    paragraph.ListFormat.BulletCharacter = '\u2022';
                    textPart.Font.FontName = "system-ui";
                    textPart.Font.FontSize = 18;
                    textPart.Font.Color = ColorObject.FromArgb(144, 148, 152);

                }

            }

            shape = slide.Shapes.FirstOrDefault(u => u.ShapeName == "imgVilla") as IShape;
            if (shape is not null)
            {
                byte[] imageData;
                string imageUrl;
                try
                {
                    imageUrl = string.Format("{0}{1}", basePath, villa.ImageUrl);
                    imageData = System.IO.File.ReadAllBytes(imageUrl);
                }
                catch (Exception)
                {
                    imageUrl = string.Format("{0}{1}", basePath, "/images/placeholder.png");
                    imageData = System.IO.File.ReadAllBytes(imageUrl);
                }
                slide.Shapes.Remove(shape);
                using MemoryStream imageStream = new(imageData);
                IPicture newPicture = slide.Pictures.AddPicture(imageStream, 60, 120, 300, 200);

            }



            MemoryStream memoryStream = new();
            presentation.Save(memoryStream);
            memoryStream.Position = 0;
            return File(memoryStream, "application/pptx", "villa.pptx");
        }


        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}

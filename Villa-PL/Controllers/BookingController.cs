using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NToastNotify;
using Store.Application.Common;
using Store.Application.Common.Interfaces;
using Store.Application.Common.Utility;
using Store.Domain.Entities;
using Stripe;
using Stripe.Checkout;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIO;
using Syncfusion.DocIORenderer;
using Syncfusion.Pdf;
using System.Drawing;
using System.Linq.Expressions;
using System.Security.Claims;
using Villa_PL.ViewModel;
using Syncfusion.Presentation;
using ListType = Syncfusion.Presentation.ListType;
using Villa_PL.Helpers;
using Villa_PL.Models;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;

namespace Villa_PL.Controllers
{
    public class BookingController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBookingRepository _bookingRepository;
        private readonly IToastNotification _toastNotification;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BookingController(IUnitOfWork unitOfWork, IBookingRepository bookingRepository, IToastNotification toastNotification,IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _bookingRepository = bookingRepository;
            _toastNotification = toastNotification;
            _webHostEnvironment = webHostEnvironment;
        }
        [Authorize]
        public IActionResult Index()
        {
            return View();
        }
        [Authorize]
        public async Task<IActionResult> FinalizingBooking(int villaId, DateOnly CheckInDate, int Nights)
        {
            var villa = await _unitOfWork.Repository<Villa>().GetByIdAsync(filter: v => v.Id == villaId, includeProperties:v=>v.VillaAmenity);
            Booking booking = new()
            {
                VillaId = villaId,
                CheckInDate = CheckInDate,
                Nights = Nights,
                Villa = villa,
                CheckOutDate = CheckInDate.AddDays(Nights),
            };

            booking.TotalCost = booking.Nights * booking.Villa.Price;
            return View(booking);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> FinalizingBooking(Booking booking)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var villa = await _unitOfWork.Repository<Villa>().GetByIdAsync(v => v.Id == booking.VillaId);
            booking.UserId = userId;

            // Ensure the correct calculation of TotalCost and Nights
            booking.TotalCost = booking.Nights * villa.Price;

            booking.Status = Const.BookingStatus.Pending;
            booking.BookingDate = DateTime.Now;

            await _unitOfWork.Repository<Booking>().CreateAsync(booking);
            await _unitOfWork.CompleteAsync();

            var domain = Request.Scheme + "://" + Request.Host.Value + "/";

            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = domain + $"booking/BookingConfirmation?bookingId={booking.Id}&TotalCost={booking.TotalCost}&Nights={booking.Nights}",
                CancelUrl = domain + $"booking/FinalizeBooking?villaId={booking.VillaId}&checkInDate={booking.CheckInDate}&nights={booking.Nights}",
            };

            options.LineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)(booking.TotalCost * 100),
                    Currency = "aed",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = villa.Name,
                    },
                },
                Quantity = 1,
            });


            var service = new SessionService();

            Session session = service.Create(options);


            await _bookingRepository.UpdateStripePaymentIntentAsync(booking.Id, session.Id, session.PaymentIntentId);
            await _unitOfWork.CompleteAsync();

            return Redirect(session.Url);
        }

        [Authorize]
        public async Task<IActionResult> BookingConfirmation(int bookingId, double TotalCost, int Nights)
        {
            var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(v => v.Id == bookingId);
            if (booking.Status == Const.BookingStatus.Pending)
            {
                var service = new SessionService();
                Session session = service.Get(booking.StripeSessionId);
                if (session.Status == "complete")
                {
                    await _bookingRepository.UpdateStatusAsync(bookingId, Const.BookingStatus.Completed, 0);
                    await _bookingRepository.UpdateStripePaymentIntentAsync(bookingId, session.Id, session.PaymentIntentId);
                    await _unitOfWork.CompleteAsync();
                }
            }
            // Create an Email object
            var email = new Email
            {
                Title = "Booking Confirmation - Store Sakr",
                Body = $@"Thank you for your booking! Your booking details are as follows: {booking.Id} Nights: {Nights} Total Cost: {TotalCost:0} AED",
                To = User.Identity.Name // Assuming the user's email is the username
            };

            
            EmailService mail = new();
            // Send the email
            mail.SendEmail(email);


            ViewBag.Nights = Nights;
            ViewBag.TotalCost = TotalCost;
            return View(bookingId);
        }


        [Authorize]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(b => b.Id == bookingId);
            if (booking is not null)
            {

                booking.Status = Const.BookingStatus.Cancelled;
                await _unitOfWork.CompleteAsync();
                _toastNotification.AddSuccessToastMessage("Cancel Booking Successful");
            }
            else
            {
                _toastNotification.AddErrorToastMessage("Booking not found");
            }
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> CompleteBooking(int bookingId)
        {
            var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(b => b.Id == bookingId);

            if (booking is not null)
            {
                booking.Status = Const.BookingStatus.Completed;

                await _unitOfWork.Repository<Booking>().UpdateAsync(booking);
                await _unitOfWork.CompleteAsync();

                _toastNotification.AddSuccessToastMessage("Complete Booking Successful");
            }
            else
            {
                _toastNotification.AddErrorToastMessage("Booking not found");
            }

            return RedirectToAction("Index", "Home");
        }
        [Authorize]
        public async Task<IActionResult> ReopenBooking(int bookingId)
        {
            var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(b => b.Id == bookingId);

            if (booking is not null)
            {
                booking.Status = Const.BookingStatus.Pending; 

                await _unitOfWork.Repository<Booking>().UpdateAsync(booking);
                await _unitOfWork.CompleteAsync();

                _toastNotification.AddSuccessToastMessage("Reopen Booking Successful");
            }
            else
            {
                _toastNotification.AddErrorToastMessage("Booking not found");
            }

            return RedirectToAction("Index", "Home");
        }

        #region API Call
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllBooking(string status)
        {
            IEnumerable<Booking> bookings;
            if (User.IsInRole(Const.Role_Admin))
            {
                bookings = await _unitOfWork.Repository<Booking>().GetAllAsync(includeProperties: v => v.Villa);

            }
            else
            {
                var claimsIdentity = new ClaimsIdentity(User.Identity);
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                bookings = await _unitOfWork.Repository<Booking>().GetAllAsync(u => u.UserId == userId, includeProperties: v => v.Villa);
            }
            if (!string.IsNullOrEmpty(status))
            {
                bookings = bookings.Where(b => b.Status.ToLower() == status.ToLower());
            }
            return Json(new { data = bookings });
        }


        #endregion

        [Authorize]
        public async Task<IActionResult> BookingDetails(int bookingId)
        {
            var bookingDetails = await _unitOfWork.Repository<Booking>().GetByIdAsync(filter: b => b.Id == bookingId,includeProperties:v=>v.Villa);
            bookingDetails.VillaNumbers = await _unitOfWork.Repository<VillaNumber>().GetAllAsync();

            if (bookingDetails != null && bookingDetails.VillaNumber == 0 && bookingDetails.Status == Const.BookingStatus.Pending)
            {
                var availableVillaNumbers = await AssignAvailableVillaNumbers(bookingDetails.VillaId);

                //bookingDetails.VillaNumbers = availableVillaNumbers.Select(villaNumber => new VillaNumber { Villa_Number = villaNumber }).ToList();
            }
            IEnumerable<VillaNumber> villaNumbers1 = new List<VillaNumber>();
            var villaNumbers = bookingDetails?.VillaNumbers;
            var villaId = bookingDetails?.VillaId;

            if (villaNumbers != null && villaId != null)
            {
                foreach (var item in villaNumbers)
                {
                    if (villaId == item.VillaId)
                    {
                        villaNumbers1 = villaNumbers1.Concat(new[] { item });
                    }
                }
            }

          
            bookingDetails.VillaNumbers = villaNumbers1;
            return View(bookingDetails);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> GenerateInvoice(int id, string downloadType)
        {
            string basePath = _webHostEnvironment.WebRootPath;

            WordDocument document = new WordDocument();

            // Load the template.
            string dataPath = basePath + @"/exports/BookingDetails.docx";
            using FileStream fileStream = new(dataPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            document.Open(fileStream, Syncfusion.DocIO.FormatType.Automatic);

            var bookingDetails = await _unitOfWork.Repository<Booking>().GetByIdAsync(b => b.Id == id, includeProperties: V => V.Villa);

            // Name
            TextSelection textSelection = document.Find("customer_name", false, true);
            WTextRange wTextRange = textSelection.GetAsOneRange();
            wTextRange.Text = bookingDetails.Name;

            //Phone
            textSelection = document.Find("customer_phone", false, true);
            wTextRange = textSelection.GetAsOneRange();
            wTextRange.Text = bookingDetails.Phone;

            //Email
            textSelection = document.Find("customer_email", false, true);
            wTextRange = textSelection.GetAsOneRange();
            wTextRange.Text = bookingDetails.Email;

            //Payment Date
            textSelection = document.Find("payment_date", false, true);
            wTextRange = textSelection.GetAsOneRange();
            wTextRange.Text = bookingDetails.PaymentDate.ToShortDateString();

            //Check In Date
            textSelection = document.Find("checkin_date", false, true);
            wTextRange = textSelection.GetAsOneRange();
            wTextRange.Text = bookingDetails.CheckInDate.ToShortDateString();

            //Check out Date
            textSelection = document.Find("checkout_date", false, true);
            wTextRange = textSelection.GetAsOneRange();
            wTextRange.Text = bookingDetails.CheckOutDate.ToShortDateString();

            //booking Total
            textSelection = document.Find("booking_total", false, true);
            wTextRange = textSelection.GetAsOneRange();
            wTextRange.Text = $"💰 Total Cost: {bookingDetails.TotalCost:F2} AED";

            //Check out Date
            textSelection = document.Find("BOOKING_Date", false, true);
            wTextRange = textSelection.GetAsOneRange();
            wTextRange.Text = "Booking Date : " + bookingDetails.BookingDate.ToShortDateString();

            //booking Number
            textSelection = document.Find("booking_Number", false, true);
            wTextRange = textSelection.GetAsOneRange();
            wTextRange.Text = $"Booking Number : {bookingDetails.Id}";

            // DateTime_Now
            textSelection = document.Find("Date_Now", false, true);
            wTextRange = textSelection.GetAsOneRange();
            DateTime dateTimeValue = DateTime.Now.Date;
            string formattedDate = dateTimeValue.Date.ToShortDateString();
            wTextRange.Text = "Date Day: " + formattedDate;

            // Assuming "Time_Now" is a placeholder in your document
            textSelection = document.Find("Time_Now", false, true);
            wTextRange = textSelection.GetAsOneRange();
            string formattedTime = DateTime.Now.ToString("hh:mm tt"); // 12-hour clock with AM/PM
            wTextRange.Text = "⏰ Time: " + formattedTime;

            WTable table = new(document);

            table.TableFormat.Borders.LineWidth = 1f;
            table.TableFormat.Borders.Color = Syncfusion.Drawing.Color.Black;
            table.TableFormat.Paddings.Top = 7f;
            table.TableFormat.Paddings.Bottom = 7f;
            table.TableFormat.Borders.Horizontal.LineWidth = 1f;

            table.ResetCells(2, 4);

            WTableRow row0 = table.Rows[0];

            // Assuming you have placeholders in your table cells like "{ICON_NIGHTS}" and "{ICON_VILLA}"
            row0.Cells[0].AddParagraph().AppendText("💤 Nights");
            row0.Cells[0].Width = 110;
            row0.Cells[1].AddParagraph().AppendText("🏠 Villa Name");
            row0.Cells[1].Width = 150;
            row0.Cells[2].AddParagraph().AppendText("💲 Price Per Night");
            row0.Cells[3].AddParagraph().AppendText("💰 Total Cost");
            row0.Cells[3].Width = 110;

            WTableRow row1 = table.Rows[1];

            // Assuming you have placeholders in your table cells like "{ICON_NIGHTS_VALUE}" and "{ICON_VILLA_NAME}"
            row1.Cells[0].AddParagraph().AppendText($"💤 {bookingDetails.Nights} nights");
            row1.Cells[0].Width = 110;
            row1.Cells[1].AddParagraph().AppendText($"🏠 {bookingDetails.Villa.Name}");
            row1.Cells[1].Width = 150;
            // Format Price Per Night as AED without $
            row1.Cells[2].AddParagraph().AppendText($"💲 {(bookingDetails.TotalCost / bookingDetails.Nights):F2} AED");
            row1.Cells[3].AddParagraph().AppendText($"💰 {bookingDetails.TotalCost:F2} AED");
            row1.Cells[3].Width = 110;


            if (bookingDetails.VillaNumber > 0)
            {
                WTableRow row2 = table.Rows[2];

                row2.Cells[0].Width = 80;
                row2.Cells[1].AddParagraph().AppendText("Villa Number - " + bookingDetails.VillaNumber.ToString());
                row2.Cells[1].Width = 220;
                row2.Cells[3].Width = 80;
            }

            WTableStyle tableStyle = document.AddTableStyle("CustomStyle") as WTableStyle;
            tableStyle.TableProperties.RowStripe = 1;
            tableStyle.TableProperties.ColumnStripe = 2;
            tableStyle.TableProperties.Paddings.Top = 2;
            tableStyle.TableProperties.Paddings.Bottom = 1;
            tableStyle.TableProperties.Paddings.Left = 5.4f;
            tableStyle.TableProperties.Paddings.Right = 5.4f;

            ConditionalFormattingStyle firstRowStyle = tableStyle.ConditionalFormattingStyles.Add(ConditionalFormattingType.FirstRow);
            firstRowStyle.CharacterFormat.Bold = true;
            firstRowStyle.CharacterFormat.TextColor = Syncfusion.Drawing.Color.FromArgb(255, 255, 255, 255);
            firstRowStyle.CellProperties.BackColor = Syncfusion.Drawing.Color.Black;

            table.ApplyStyle("CustomStyle");


            TextBodyPart textBodyPart = new(document);

            textBodyPart.BodyItems.Add(table);

            document.Replace("<ADDTABLEHERE>", textBodyPart, false, false);



            using DocIORenderer renderer = new();
            MemoryStream stream= new();


            if(downloadType =="word")
            {
                document.Save(stream, Syncfusion.DocIO.FormatType.Docx);
                stream.Position = 0;

                return File(stream, "application/docx", "BookingDetails.docx");
            }
            else
            {
                PdfDocument pdfDocument=renderer.ConvertToPDF(document);
                pdfDocument.Save(stream);
                stream.Position = 0;
                return File(stream, "application/pdf", "BookingDetails.pdf");
            }


                     
        }
      

        private async Task<List<int>> AssignAvailableVillaNumbers(int villaId)
        {
            List<int> availableVilla = new();
            var allVillaBookings = await _unitOfWork.Repository<Booking>().GetAllAsync(v => v.VillaId == villaId);

            var completedBookings = await _unitOfWork.Repository<Booking>().GetAllAsync(v => v.VillaId == villaId && v.Status == Const.BookingStatus.Completed);

            foreach (var villaBooking in allVillaBookings)
            {
                var isCompleted = completedBookings.Any(cb => cb.Id == villaBooking.Id);

                if (!isCompleted)
                {
                    availableVilla.Add(villaBooking.VillaNumber);
                }
            }
            return availableVilla;
        }

        #region Check

        [HttpPost]
        [Authorize(Roles = Const.Role_Admin)]
        public async Task<IActionResult> CancelBooking(Booking booking)
        {
            await _bookingRepository.UpdateStatusAsync(booking.Id, Const.BookingStatus.Cancelled, booking.VillaNumber);
            _toastNotification.AddSuccessToastMessage("Booking Cancelled Successfully");
            return RedirectToAction(nameof(BookingDetails), new { bookingId = booking.Id });
        }

        [HttpPost]
        [Authorize(Roles = Const.Role_Admin)]
        public async Task<IActionResult> CheckOut(Booking booking)
        {
            await _bookingRepository.UpdateStatusAsync(booking.Id, Const.BookingStatus.Completed, booking.VillaNumber);
            _toastNotification.AddSuccessToastMessage("Booking Completed Successfully");
            return RedirectToAction(nameof(BookingDetails), new { bookingId = booking.Id });
        }
        #endregion

    }
}

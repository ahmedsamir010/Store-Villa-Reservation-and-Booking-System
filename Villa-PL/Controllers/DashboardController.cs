using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store.Application.Common.Interfaces;
using Store.Domain.Entities;
using Villa_PL.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Store.Application.Common;
using Store.Application.Common.Utility;

namespace Villa_PL.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        private const int PreviousMonthOffset = -1;

        public DashboardController(IBookingRepository bookingRepository, IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetTotalBookingRadialCharts()
        {
            var totalBooking = await _unitOfWork.Repository<Booking>().GetAllAsync(b => b.Status == Const.BookingStatus.Completed);

            var countByCurrentMonth = totalBooking.Count(t => t.BookingDate >= GetMonthStartDate(DateTime.Now.Month) && t.BookingDate <= DateTime.Now);
            var countByPreviousMonth = totalBooking.Count(t => t.BookingDate >= GetMonthStartDate(DateTime.Now.Month + PreviousMonthOffset) && t.BookingDate < GetMonthStartDate(DateTime.Now.Month));

            RadialBarChartVM radialBarChartVM = new();

            int increaseDecreaseRatio = 100;

            if (countByPreviousMonth != 0)
            {
                increaseDecreaseRatio = Convert.ToInt32((countByCurrentMonth - countByPreviousMonth) / (double)countByPreviousMonth * 100);
            }

            radialBarChartVM.TotalCount = totalBooking.Count();
            radialBarChartVM.CountInCurrentMonth = countByCurrentMonth;
            radialBarChartVM.HasRatioIncreased = DateTime.Now.Month > DateTime.Now.AddMonths(PreviousMonthOffset).Month;
            radialBarChartVM.Series = new List<int> { increaseDecreaseRatio };

            return Json(radialBarChartVM);
        }

        public async Task<IActionResult> GetTotalUsersRadialCharts()
        {
            var totalUsers = await _userManager.Users.ToListAsync();

            var countByCurrentMonth = totalUsers.Count(u => u.CreatedAt >= GetMonthStartDate(DateTime.Now.Month) && u.CreatedAt <= DateTime.Now);
            var countByPreviousMonth = totalUsers.Count(u => u.CreatedAt >= GetMonthStartDate(DateTime.Now.Month + PreviousMonthOffset) && u.CreatedAt < GetMonthStartDate(DateTime.Now.Month));

            RadialBarChartVM radialBarChartVM = new();

            int increaseDecreaseRatio = 100;

            if (countByPreviousMonth != 0)
            {
                increaseDecreaseRatio = Convert.ToInt32((countByCurrentMonth - countByPreviousMonth) / (double)countByPreviousMonth * 100);
            }

            radialBarChartVM.TotalCount = totalUsers.Count();
            radialBarChartVM.CountInCurrentMonth = countByCurrentMonth;
            radialBarChartVM.HasRatioIncreased = DateTime.Now.Month > DateTime.Now.AddMonths(PreviousMonthOffset).Month;
            radialBarChartVM.Series = new List<int> { increaseDecreaseRatio };

            return Json(radialBarChartVM);
        }

        public async Task<IActionResult> GetTotalRevenueRadialCharts()
        {
            var totalBookings = await _unitOfWork.Repository<Booking>().GetAllAsync(b => b.Status == Const.BookingStatus.Completed);
            var revenueByCurrentMonth = totalBookings
                .Where(b => b.BookingDate >= GetMonthStartDate(DateTime.Now.Month) && b.BookingDate <= DateTime.Now)
                .Sum(b => (decimal)b.TotalCost);

            var revenueByPreviousMonth = totalBookings
                .Where(b => b.BookingDate >= GetMonthStartDate(DateTime.Now.Month + PreviousMonthOffset) && b.BookingDate < GetMonthStartDate(DateTime.Now.Month))
                .Sum(b => (decimal)b.TotalCost);
            RadialBarChartVM radialBarChartVM = new();

            int increaseDecreaseRatio = 100;

            if (revenueByPreviousMonth != 0)
            {
                increaseDecreaseRatio = Convert.ToInt32((revenueByCurrentMonth - revenueByPreviousMonth) / revenueByPreviousMonth * 100);
            }

            radialBarChartVM.TotalCount = totalBookings.Count();
            radialBarChartVM.CountInCurrentMonth = revenueByCurrentMonth;
            radialBarChartVM.HasRatioIncreased = DateTime.Now.Month > DateTime.Now.AddMonths(PreviousMonthOffset).Month;
            radialBarChartVM.Series = new List<int> { increaseDecreaseRatio };

            return Json(radialBarChartVM);
        }
        public async Task<IActionResult> GetBookingPieCharts()
        {
            var totalBookingsTask = _unitOfWork.Repository<Booking>().GetAllAsync(u => u.BookingDate >= DateTime.Now.AddDays(-30) &&
               (u.Status != Const.BookingStatus.Pending || u.Status == Const.BookingStatus.Cancelled));

            var totalBookings = await totalBookingsTask;

            var customerWithOneBooking = totalBookings.GroupBy(b => b.UserId).Where(x => x.Count() == 1).Select(x => x.Key).ToList();

            int bookingsByNewCustomer = customerWithOneBooking.Count();
            int bookingsByReturningCustomer = totalBookings.Count() - bookingsByNewCustomer;

            PieChartVM PieChartDto = new()
            {
                Label = new string[] { "New Customer Bookings", "Returning Customer Bookings" },
                Series = new decimal[] { bookingsByNewCustomer, bookingsByReturningCustomer }
            };

            return Json(PieChartDto);
        }
        public async Task<IActionResult> GetMemberAndBookingChartData()
        {
            var bookings = await _unitOfWork.Repository<Booking>()
                .GetAllAsync(t => t.BookingDate >= DateTime.Now.AddDays(-30) && t.BookingDate <= DateTime.Now);

            var groupedBookings = bookings.GroupBy(b => b.BookingDate.Date);

            var bookingChartData = groupedBookings.Select(group => new
            {
                BookingDate = group.Key,
                NumberOfBookings = group.Count()
            });

            var totalUsers = await _userManager.Users
                .Where(u => u.CreatedAt <= DateTime.Now.AddDays(-30))
                .ToListAsync();

            var groupedCustomers = totalUsers.GroupBy(u => u.CreatedAt.Date);

            var customersChartData = groupedCustomers.Select(group => new
            {
                DateTime = group.Key,
                NumberOfCustomers = group.Count()
            });

            var leftJoin = bookingChartData.GroupJoin(customersChartData, booking => booking.BookingDate, customer => customer.DateTime,
                (booking, customer) => new
                {
                    booking.BookingDate,
                    booking.NumberOfBookings,
                    NumberOfCustomers = customer.Select(c => c.NumberOfCustomers).FirstOrDefault()
                });


            var rightJoin = customersChartData.GroupJoin(bookingChartData, customer => customer.DateTime, booking => booking.BookingDate,
              (customer, bookings) => new
              {
                  customer.DateTime,
                  NumberOfBookings = bookings.Sum(x => x.NumberOfBookings),
                  customer.NumberOfCustomers
              });

            var mergedResult = leftJoin
                .Select(item => new
                {
                    DateTime = item.BookingDate,
                    NumberOfBookings = item.NumberOfBookings,
                    NumberOfCustomers = item.NumberOfCustomers
                })
                .Union(rightJoin.Select(item => new
                {
                    DateTime = item.DateTime,
                    NumberOfBookings = item.NumberOfBookings,
                    NumberOfCustomers = item.NumberOfCustomers
                }))
                .GroupBy(item => item.DateTime)
                .Select(group => new
                {
                    DateTime = group.Key,
                    NumberOfBookings = group.Sum(item => item.NumberOfBookings),
                    NumberOfCustomers = group.Sum(item => item.NumberOfCustomers)
                })
                .ToList();

            var newCustomerData = mergedResult.Select(x => x.NumberOfCustomers).ToArray();
            var newBookingData = mergedResult.Select(x => x.NumberOfBookings).ToArray();
            var categories = mergedResult.Select(x => x.DateTime.ToString("MM/dd/yyyy")).ToArray();

          
            List<ChartData> chartDataList = new()
            {
                new ChartData
                {
                    Name="New Booking",
                    data=newBookingData
                },

                new ChartData
                {

                    Name="New Customers",
                    data=newCustomerData
                },
            };

            LineChartVM lineChartVM = new()
            {
                Categories = categories,
                Series = chartDataList

            };
            return Json(lineChartVM);
        }


        private DateTime GetMonthStartDate(int month)
        {
            return new DateTime(DateTime.Now.Year, month, 1);
        }
    }
}

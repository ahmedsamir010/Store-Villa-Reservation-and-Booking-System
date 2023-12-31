using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Store.Application.Common.Interfaces;
using Store.Application.Common.Utility;
using Store.Domain.Entities;
using Store.Infrastructre.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Store.Infrastructre.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public BookingRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        public async Task UpdateStripePaymentIntentAsync(int bookingId, string sessionId, string paymentIntentId)
        {
            var booking = await _dbContext.bookings.FindAsync(bookingId);
            if (booking != null)
            {
                if(!string.IsNullOrEmpty(sessionId))
                {
                    booking.StripeSessionId = sessionId;
                }
                if (!string.IsNullOrEmpty(sessionId))
                {
                   booking.StripePaymentIntentId = paymentIntentId;
                    booking.PaymentDate=DateTime.Now;
                    booking.IsPaymentSuccessful=true;
                }
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task UpdateStatusAsync(int bookingId, string bookingStatus,int villaNumber=0)
        {
            var booking = await _dbContext.bookings.FindAsync(bookingId);
            if (booking != null)
            {
                booking.Status = bookingStatus;
                if (bookingStatus == Const.BookingStatus.Completed)
                {
                     booking.VillaNumber=villaNumber;
                    booking.ActualCheckOutDate = DateTime.Now;
                }
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}

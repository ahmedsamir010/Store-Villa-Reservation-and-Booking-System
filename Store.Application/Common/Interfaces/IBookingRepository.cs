using Store.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Application.Common.Interfaces
{
    public interface IBookingRepository
    {
        Task UpdateStatusAsync(int bookingId,string bookingStatus, int villaNumber);
        Task UpdateStripePaymentIntentAsync(int bookingId,string sessionId,string paymentIntentId);
    }
}

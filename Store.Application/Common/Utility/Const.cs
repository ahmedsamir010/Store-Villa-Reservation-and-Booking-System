using Store.Domain.Entities;

namespace Store.Application.Common.Utility
{
    public static class Const
    {
        public const string Role_Admin = "Admin";
        public const string Role_Customer = "Customer";

        public static class BookingStatus
        {
            public const string Pending = "Pending";
            public const string Completed = "Completed";
            public const string Cancelled = "Cancelled";
        }

        public static int CalculateAvailableRoomsForNights(int villaId,
            IEnumerable<VillaNumber> villaNumberList, DateOnly checkInDate, int nights,
           IEnumerable<Booking> bookings)
        {
            List<int> bookingInDate = new();
            int finalAvailableRoomForAllNights = int.MaxValue;
            var roomsInVilla = villaNumberList.Where(x => x.VillaId == villaId).Count();

            for (int i = 0; i < nights; i++)
            {
                var villasBooked = bookings.Where(u => u.CheckInDate <= checkInDate.AddDays(i)
                && u.CheckOutDate > checkInDate.AddDays(i) && u.VillaId == villaId);

                foreach (var booking in villasBooked)
                {
                    if (!bookingInDate.Contains(booking.Id))
                    {
                        bookingInDate.Add(booking.Id);
                    }
                }

                var totalAvailableRooms = roomsInVilla - bookingInDate.Count;
                if (totalAvailableRooms == 0)
                {
                    return 0;
                }
                else
                {
                    if (finalAvailableRoomForAllNights > totalAvailableRooms)
                    {
                        finalAvailableRoomForAllNights = totalAvailableRooms;
                    }
                }
            }

            return finalAvailableRoomForAllNights;
        }


    }
}

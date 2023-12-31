using Microsoft.AspNetCore.Mvc.Rendering;
using Store.Domain.Entities;

namespace Villa_PL.ViewModel
{
    public class HomeVM
    {
        public IEnumerable<Villa>? villaList { get; set; }

        public DateOnly CheckInDate { get; set; }
        public DateOnly? CheckOutDate { get; set; }

        public int Nights { get; set; }
    }
}

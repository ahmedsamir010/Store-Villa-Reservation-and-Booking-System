using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Store.Domain.Entities;

namespace Villa_PL.ViewModel
{
    public class AmenityVM
    {
        public Amenity amenity { get; set; }
        public IEnumerable<Amenity> amenities { get; set; }

        [ValidateNever]
        public IEnumerable<SelectListItem> villaList { get; set; }
    }
}

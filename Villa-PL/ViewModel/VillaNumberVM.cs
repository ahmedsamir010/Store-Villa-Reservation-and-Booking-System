using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Store.Domain.Entities;

namespace Villa_PL.ViewModel
{
    public class VillaNumberVM
    {
        public VillaNumber? villaNumber { get; set; }

        [ValidateNever]
        public IEnumerable<SelectListItem> villaList { get; set; }
        [ValidateNever]
        public int? SelectedVillaId { get; set; }

        public IEnumerable<VillaNumber> VillaNumbers { get; set; }

    }
}

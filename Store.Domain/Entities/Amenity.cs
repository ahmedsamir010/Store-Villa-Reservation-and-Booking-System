using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Domain.Entities
{
    public class Amenity : BaseEntity
    {


        [Display(Name ="Amenity Name")]
        public required string Name { get; set; }

        public string? Description { get; set; }

        [ValidateNever]
        public Villa villa { get; set; }

        [Display(Name ="Villa Name")]
        [ForeignKey("Villa")]
        public int VillaId { get; set; }
    }
}

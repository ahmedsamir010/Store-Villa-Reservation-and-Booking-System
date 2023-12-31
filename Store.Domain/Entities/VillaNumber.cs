using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;


namespace Store.Domain.Entities
{
    public class VillaNumber : BaseEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // No automatic generation of values
        [Display(Name = "Villa Number")]
        public int Villa_Number { get; set; }
        [Display(Name = "Villa Name")]
        [ForeignKey("Villa")]
        public int VillaId { get; set; }
        [ValidateNever]
        public Villa Villa { get; set; }
        public string? SpecialDetails { get; set; }
    

    
    }
}

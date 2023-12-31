using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Domain.Entities
{
    public class Villa
    {
        public int Id { get; set; }
        [MaxLength(50)]
        public required string Name { get; set; }
        public string? Description { get; set; }
        [Display(Name = "Price Per Night")]
        [Range(100,100000 )]
        public double Price { get; set; }

        public int Sqfd { get; set; }
        [Range(1,20)]
        public int Occupany { get; set; }

        [Display(Name="Image Url")]
        public string? ImageUrl { get; set; }

        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdateDate { get; set; }

    }

}

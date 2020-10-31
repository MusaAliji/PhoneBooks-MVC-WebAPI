using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PhoneBooks.Models
{
    public class PhoneBookModel
    {
        public string ID { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        [RegularExpression(@"^(Work|Cellphone|Home)$")]
        public string Type { get; set; }
        [Required]
        [RegularExpression(@"^[+]{1}[0-9]{3}[-0-9]*$")]
        public string Number { get; set; }
    }
}
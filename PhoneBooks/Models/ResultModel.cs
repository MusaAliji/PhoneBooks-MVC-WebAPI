using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhoneBooks.Models
{
    public class ResultModel
    {
        public int HttpStatus { get; set; }
        public string ErrorMessage { get; set; }
        public string SuccessMesage { get; set; }
    }
}
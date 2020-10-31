using PhoneBooks.Custom;
using PhoneBooks.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Configuration;
using System.Web.Http;

namespace PhoneBooks.Controllers
{
    public class PhoneBookController : ApiController
    {
        private static string filePath = Path.Combine(HttpContext.Current.Server.MapPath(WebConfigurationManager.AppSettings["path"]), "PhoneBooks.csv");
        private static string delimeter = ",";
        private static object updateLock = new object();
        private static object deleteLock = new object();
        private static object sortLock = new object();

        // POST PhoneBook/Create
        /// <summary>
        /// Create a file in Content/PhoneBooks folder if not exists and add header line with ID, Name, Type, Number
        /// Also create a Phone Book with data in model
        /// </summary>
        /// <param name="model">Name, Type, Number</param>
        /// <returns> ResultModel that contain (HttpStatus, ErrorMessage (if has error), SuccessMessage (if has been completed successfully))</returns>
        [Route("PhoneBook/Create")]
        [HttpPost]
        public ResultModel Create([FromBody] PhoneBookModel model)
        {
            if (ModelState.IsValid)
            {
                StringBuilder header = new StringBuilder();
                StringBuilder content = new StringBuilder();
                PropertyInfo[] properties = typeof(PhoneBookModel).GetProperties();
                List<string> headerRows = new List<string>();

                try
                {
                    if (!new PhoneBookHelper().FileExists(filePath) || (new PhoneBookHelper().FileExists(filePath) & new PhoneBookHelper().FileLength(filePath) <= 0))
                    {
                        foreach (PropertyInfo property in properties)
                        {
                            headerRows.Add(property.Name);
                        }

                        header.AppendLine(string.Join(delimeter, headerRows));
                        headerRows.Clear();

                        new PhoneBookHelper().AppendLine(filePath, header, true);
                    }

                    content.AppendLine(string.Concat(Guid.NewGuid().ToString(), ",", model.Name, ",", model.Type, ",", model.Number));

                    new PhoneBookHelper().AppendLine(filePath, content, false);

                    return new ResultModel() { HttpStatus = (int)HttpStatusCode.OK, SuccessMesage = "Phone Book has been successfully added." };
                }
                catch (Exception e)
                {
                    return new ResultModel() { HttpStatus = (int)HttpStatusCode.InternalServerError, ErrorMessage = e.Message };
                }
            }
            return new ResultModel() { HttpStatus = (int)HttpStatusCode.BadRequest, ErrorMessage = "Fields are not filled correctly. Name, Type and Number fields are required and Type must be (Work, Cellphone or Home) also Number must begin with (+111) and can contain (-) !!!" };
        }

        //POST /PhoneBook/Update
        /// <summary>
        /// Update a Phone Book finded by ID.
        /// </summary>
        /// <param name="model">ID, Name, Type, Number</param>
        /// <returns> ResultModel that contain (HttpStatus, ErrorMessage (if has error), SuccessMessage (if has been completed successfully))</returns>
        [Route("PhoneBook/Update")]
        [HttpPost]
        public ResultModel Update([FromBody] PhoneBookModel model)
        {
            if (string.IsNullOrEmpty(model.ID))
                return new ResultModel() { HttpStatus = (int)HttpStatusCode.BadRequest, ErrorMessage = "Please define an ID for update!!!" };

            if (ModelState.IsValid)
            {
                if (!(new PhoneBookHelper().FileExists(filePath)))
                    return new ResultModel() { HttpStatus = (int)HttpStatusCode.BadRequest, ErrorMessage = "You can not update an empty Phone Book, please create a phone book firstly!!!" };

                bool updateLocked = false;
                try
                {
                    Monitor.Enter(updateLock, ref updateLocked);
                    var lines = File.ReadAllLines(filePath);
                    int? lineNumber = null;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Contains(model.ID))
                        {
                            lineNumber = i;
                            break;
                        }
                    }

                    if (lineNumber == null)
                        return new ResultModel() { HttpStatus = (int)HttpStatusCode.BadRequest, ErrorMessage = "The Phone Book can not be find!!!" };

                    lines[(int)lineNumber] = string.Concat(model.ID, ",", model.Name, ",", model.Type, ",", model.Number);

                    File.WriteAllLines(filePath, lines);
                    if (updateLocked)
                        Monitor.Exit(updateLock);
                    return new ResultModel() { HttpStatus = (int)HttpStatusCode.OK, SuccessMesage = "Phone Book has been successfully updated." };
                }
                catch (Exception e)
                {
                    if (updateLocked)
                        Monitor.Exit(updateLock);
                    return new ResultModel() { HttpStatus = (int)HttpStatusCode.InternalServerError, ErrorMessage = e.Message };
                }
            }
            return new ResultModel() { HttpStatus = (int)HttpStatusCode.BadRequest, ErrorMessage = "Fields are not filled correctly. Name, Type and Number fields are required and Type must be (Work, Cellphone or Home) also Number must begin with (+111) and can contain (-) !!!" };
        }

        //DELETE PhoneBook/Delete/{ID}
        /// <summary>
        /// Delete a Phone Book from file, if file exists and if Phone Book finded by ID param.
        /// </summary>
        /// <param name="ID">The unique user ID that automatically generated GUID from create</param>
        /// <returns> ResultModel that contain (HttpStatus, ErrorMessage (if has error), SuccessMessage (if has been completed successfully))</returns>
        [Route("PhoneBook/Delete/{ID?}")]
        [HttpDelete]
        public ResultModel Delete([FromUri] string ID)
        {
            if (string.IsNullOrEmpty(ID))
                return new ResultModel() { HttpStatus = (int)HttpStatusCode.BadRequest, ErrorMessage = "Please define an ID for delete!!!" };

            if (!(new PhoneBookHelper().FileExists(filePath)))
                return new ResultModel() { HttpStatus = (int)HttpStatusCode.BadRequest, ErrorMessage = "You can not delete an empty Phone Book, please create a phone book firstly!!!" };


            bool deleteLocked = false;
            try
            {
                Monitor.Enter(deleteLock, ref deleteLocked);

                var lines = File.ReadAllLines(filePath);
                int? lineNumber = null;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(ID))
                    {
                        lineNumber = i;
                        break;
                    }
                }

                if (lineNumber == null)
                    return new ResultModel() { HttpStatus = (int)HttpStatusCode.BadRequest, ErrorMessage = "The Phone Book has not been found!!!" };
                var list = new List<string>(lines);
                list.RemoveAt((int)lineNumber);
                lines = list.ToArray();

                File.WriteAllLines(filePath, lines);
                if (deleteLocked)
                    Monitor.Exit(deleteLock);
                return new ResultModel() { HttpStatus = (int)HttpStatusCode.OK, SuccessMesage = "Phone Book has been successfully deleted." };
            }
            catch (Exception e)
            {
                if (deleteLocked)
                    Monitor.Exit(deleteLock);
                return new ResultModel() { HttpStatus = (int)HttpStatusCode.InternalServerError, ErrorMessage = e.Message };
            }
        }

        // GET PhoneBook/Sort
        /// <summary>
        /// Sort the list of file depended on params.
        /// </summary>
        /// <param name="sortString">Accepted params: Name, Type, Number</param>
        /// <param name="direction">Accepted params: Asc, Desc</param>
        /// <returns> ResultModel that contain (HttpStatus, ErrorMessage (if has error), SuccessMessage (if has been completed successfully))</returns>
        [Route("PhoneBook/Sort")]
        [HttpGet]
        public ResultModel Sort([FromUri] string sortString, string direction)
        {
            if (string.IsNullOrEmpty(sortString) || string.IsNullOrEmpty(direction))
                return new ResultModel() { HttpStatus = (int)HttpStatusCode.BadRequest, ErrorMessage = "Please enter column and direction to sort Phone Book!!!" };

            if (!sortString.Equals("Name", StringComparison.OrdinalIgnoreCase) && !sortString.Equals("Type", StringComparison.OrdinalIgnoreCase) && !sortString.Equals("Number", StringComparison.OrdinalIgnoreCase))
                return new ResultModel() { HttpStatus = (int)HttpStatusCode.BadRequest, ErrorMessage = "Valid names for sort is Name, Type or Number!!!" };
            
            if (!direction.Equals("Asc", StringComparison.OrdinalIgnoreCase) && !direction.Equals("Desc", StringComparison.OrdinalIgnoreCase))
                return new ResultModel() { HttpStatus = (int)HttpStatusCode.BadRequest, ErrorMessage = "Valid direction for sort is Asc or Desc!!!" };

            bool sortLocked = false;
            try
            {
                Monitor.Enter(sortLock, ref sortLocked);

                var lines = File.ReadAllLines(filePath);
                var data = lines.Skip(1);
                var sorted = data.Select(_ => new
                {
                    Name = _.Split(',')[1],
                    Type = _.Split(',')[2],
                    Number = _.Split(',')[3],
                    Line = _
                });

                IEnumerable<string> s = sorted.OrderBy(_ => _.Name).Select(_ => _.Line);
                if (sortString.Equals("Name", StringComparison.OrdinalIgnoreCase))
                    s = direction.Equals("Asc", StringComparison.OrdinalIgnoreCase) ? sorted.OrderBy(_ => _.Name).Select(_ => _.Line) : sorted.OrderByDescending(_ => _.Name).Select(_ => _.Line);

                if (sortString.Equals("Type", StringComparison.OrdinalIgnoreCase))
                    s = direction.Equals("Asc", StringComparison.OrdinalIgnoreCase) ? sorted.OrderBy(_ => _.Type).Select(_ => _.Line) : sorted.OrderByDescending(_ => _.Type).Select(_ => _.Line);

                if (sortString.Equals("Number", StringComparison.OrdinalIgnoreCase))
                    s = direction.Equals("Asc", StringComparison.OrdinalIgnoreCase) ? sorted.OrderBy(_ => _.Number).Select(_ => _.Line) : sorted.OrderByDescending(_ => _.Number).Select(_ => _.Line);

                lines = lines.Take(1).Concat(s).ToArray();
                File.WriteAllLines(filePath, lines);
                if (sortLocked)
                    Monitor.Exit(sortLock);
                return new ResultModel() { HttpStatus = (int)HttpStatusCode.OK, SuccessMesage = "Phone Book has been sorted." };
            }
            catch
            {
                if (sortLocked)
                    Monitor.Exit(sortLock);
                return new ResultModel() { HttpStatus = (int)HttpStatusCode.OK, SuccessMesage = "An error occured while sorting!!!" };
            }           
        }
    }
}

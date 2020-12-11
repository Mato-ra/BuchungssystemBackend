using System;
using System.Web;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore;
using AisBuchung_Api.Models;
using Microsoft.AspNetCore.Http;
using JsonSerializer;

namespace AisBuchung_Api.Controllers
{
    [Route("login")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly AuthModel auth;

        public LoginController()
        {
            auth = new AuthModel();
        }

        [HttpPost]
        public ActionResult<IEnumerable<string>> GetLoggedInOrganizer(LoginPost loginPost)
        {
            var result = auth.GetLoggedInOrganizerData(loginPost);
            if (result == null)
            {
                result = auth.GetPermissions(loginPost);
                var response = Content(result, "application/json");
                response.StatusCode = 401;
                return response;
            }
            else
            {

                result = Json.MergeObjects(new string[] { result, auth.GetPermissions(loginPost) }, true);
                if (loginPost.ml != null && loginPost.pw != null)
                {
                    var token = auth.BuildToken(loginPost.ml, loginPost.HashPassword(loginPost.pw));
                    if (token != null)
                    {
                        result = Json.AddKeyValuePair(result, "token", token, true);
                    }
                }

                if (loginPost.id > 0 && loginPost.pw != null)
                {
                    var token = auth.BuildToken(loginPost.id, loginPost.HashPassword(loginPost.pw));
                    if (token != null)
                    {
                        result = Json.AddKeyValuePair(result, "token", token, true);
                    }
                }

                return Content(result, "application/json");
            }
        }
    }
}

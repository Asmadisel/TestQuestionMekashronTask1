using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security;
using System.Text;
using System.Text.Json;
using static TestQuestionMekashronTask1.Model.UserResponseRecord;

namespace TestQuestionMekashronTask1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// get method.
        /// </summary>
        public void OnGet()
        {

        }

        /// <summary>
        /// Async post-method of registration.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostRegisterAsync([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
            {
                return new JsonResult(new { success = false, message = "Login and password are required." });
            }

            try
            {
                UserResponse? user = await CallRegisterNewCustomerAsync(request.Login, request.Password);

                if (user != null)
                {
                    return new JsonResult(new
                    {
                        success = true,
                        message = $"Login successful! Your EntityId is: {user.EntityId}"
                    });
                }
                else
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Error: Invalid email or password."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while SOAP-service calling.");
                return new JsonResult(new { success = false, message = "Server error. Please try later." });
            }
        }

        /// <summary>
        /// Call soap-method "Login"
        /// </summary>
        /// <param name="userName">UserName</param>
        /// <param name="password">UserPassword</param>
        /// <returns>UserResponse.</returns>
        private async Task<UserResponse?> CallRegisterNewCustomerAsync(string userName, string password)
        {
            var soapEnvelope = $@"<soapenv:Envelope 
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
                xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
                xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" 
                xmlns:urn=""urn:ICUTech.Intf-IICUTech"">
               <soapenv:Header/>
               <soapenv:Body>
                  <urn:Login soapenv:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
                     <UserName xsi:type=""xsd:string"">{XmlEncode(userName)}</UserName>
                     <Password xsi:type=""xsd:string"">{XmlEncode(password)}</Password>
                     <IPs xsi:type=""xsd:string"">?</IPs>
                  </urn:Login>
               </soapenv:Body>
            </soapenv:Envelope>";

            using var client = new HttpClient();

            var contentBytes = Encoding.UTF8.GetBytes(soapEnvelope);
            var content = new ByteArrayContent(contentBytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml");
            content.Headers.Add("SOAPAction", "urn:ICUTech.Intf-IICUTech#Login"); 

            try
            {
                var response = await client.PostAsync("http://isapi.mekashron.com/icu-tech/icutech-test.dll/soap/IICUTech", content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return ParseLoginResponse(responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SOAP call failed.");
                return null;
            }
        }

        /// <summary>
        /// Helper method for parsing the response.
        /// </summary>
        /// <param name="soapResponse"></param>
        /// <returns></returns>
        private UserResponse? ParseLoginResponse(string soapResponse)
        {
            const string returnTagStart = "<return";
            const string returnTagEnd = "</return>";

            int startIndex = soapResponse.IndexOf(returnTagStart);
            if (startIndex == -1)
            {
                _logger.LogWarning("SOAP response does not contain '<return>' tag.");
                return null;
            }

            startIndex = soapResponse.IndexOf('>', startIndex) + 1;
            int endIndex = soapResponse.IndexOf(returnTagEnd, startIndex);

            if (endIndex == -1)
            {
                _logger.LogWarning("SOAP response is malformed.");
                return null;
            }

            string jsonString = soapResponse.Substring(startIndex, endIndex - startIndex).Trim();

            try
            {
                using var jsonDoc = JsonDocument.Parse(jsonString);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("ResultCode", out JsonElement resultCodeElement))
                {
                    int resultCode = resultCodeElement.GetInt32();
                    if (resultCode == -1)
                    {
                        _logger.LogWarning("Login failed: User not found or invalid credentials.");
                        return null; 
                    }

                    return null;
                }

                var user = JsonSerializer.Deserialize<UserResponse>(jsonString);
                return user;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON from SOAP response.");
                return null;
            }
        }

        private static string XmlEncode(string input)
        {
            return SecurityElement.Escape(input) ?? string.Empty;
        }
    }

    public class RegisterRequest
    {
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}


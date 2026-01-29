using aia_core.Entities;
using aia_core.Model.Mobile.Response;
using aia_core.UnitOfWork;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace aia_core
{
    public static class Utils
    {
        public static DateTime GetDefaultDate()
        {
            return DateTime.UtcNow.AddHours(6).AddMinutes(30);
        }

        public static DateTime ConvertUtcDateToMMDate(DateTime utcDate)
        {
            return utcDate.AddHours(6).AddMinutes(30);
        }

        public static string GetPaymentFrequency(string? code)
        {
            var frequency = "";

            switch (code)
            {
                case "01": frequency = "Annually"; break;
                case "02": frequency = "Semi Annually"; break;
                case "04": frequency = "Quarterly"; break;
                case "12": frequency = "Monthly"; break;
                default:
                    frequency = code; break;
            }

            return frequency;
        }

        public static string[] GetPolicyStatus()
        {
            string[] statusList = { "IF", "LA", "SU", "PS", "UW" };

            return statusList;
        }

        public static string[] GetActivePolicyStatus()
        {
            string[] statusList = { "IF" };

            return statusList;
        }

        public static string[] GetPremiumStatus()
        {
            string[] statusList = { "PU" };

            return statusList;
        }

        public static string GetGender(string? code)
        {
            var gender = "";

            switch (code)
            {
                case "M": gender = "Male"; break;
                case "F": gender = "Female"; break;
                default:
                    gender = code; break;
            }

            return gender;
        }

        public static string GetMaritalStatus(string? code)
        {
            var maritalStatus = "";

            switch (code)
            {
                case "M": maritalStatus = "Married"; break;
                case "S": maritalStatus = "Single"; break;
                case "D": maritalStatus = "Divorced"; break;
                case "W": maritalStatus = "Widow"; break;
                default:
                    maritalStatus = code; break;
            }

            return maritalStatus;

        }

        public static int GetNumberOfDaysForPolicyDue(DateTime paidToDate)
        {
            var numberOfDays = 0;
            try
            {
                var dateDiff = paidToDate - Utils.GetDefaultDate().Date;
                numberOfDays = dateDiff.Days;                
            }
            catch { }

            
            return numberOfDays;
        }

        public static string ConcatPlusPhoneNumber(string phoneNumber, bool? isConcatPlus = true)
        {
            if(isConcatPlus == true) return phoneNumber.StartsWith("+") ? phoneNumber : $"+{phoneNumber}";
            
            return phoneNumber;
        }

        public static string GenerateOktaUserName()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var bytes = new byte[20];

            using (var random = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                random.GetBytes(bytes);
                return new string(bytes.Select(x => chars[x % chars.Length]).ToArray());
            }
        }

        public static string GenerateOtpToken(Guid memberId, string oktaId, string sendTo, EnumOtpType otpType, string issuer, string audience, string secretKey)
        {
            try
            {
                var claims = new System.Security.Claims.Claim[]
                {
                    new System.Security.Claims.Claim($"{EnumOtpClaims.mid}", $"{memberId}"),
                    new System.Security.Claims.Claim($"{EnumOtpClaims.oktaid}", $"{oktaId}"),
                    new System.Security.Claims.Claim($"{EnumOtpClaims.target}", $"{sendTo}"),
                    new System.Security.Claims.Claim($"{EnumOtpClaims.type}", $"{otpType}")
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddMinutes(10), signingCredentials: creds);

                var otpToken = new JwtSecurityTokenHandler().WriteToken(token);
                return otpToken;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// generated otp
        /// </summary>
        /// <param name="length">default(6)</param>
        /// <returns></returns>
        #region #generate-otp
        public static string GenerateOTP([System.ComponentModel.DataAnnotations.Range(3, 99)] short length = 6)
        {
            var retry = 3;
            var otp = "";
            GenerateOtp:

            var chars = "0123456789";
            var bytes = new byte[length];

            using (var random = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                random.GetBytes(bytes);
                otp = new string(bytes.Select(x => chars[x % chars.Length]).ToArray());
                if (string.IsNullOrEmpty(otp)) { if (retry >= 0) { retry--; goto GenerateOtp; } }
            }

            return otp;
        }
        #endregion

        /// <summary>
        /// send message to phone number
        /// </summary>
        /// <param name="phone"></param>
        /// <param name="message"></param>
        /// <param name="token"></param>
        #region #send-sms
        public static void SendSms(string number, string message, string token)
        {
            try
            {
                var from = $"AIA Myanmar";
                var payload = new
                {                  
                    from = from,
                    to = number,
                    message = $"[AIA] {message}"
                };

                var API_URL = AppSettingsHelper.GetSetting("SmsPoh:API_URL");
                var BEARER = AppSettingsHelper.GetSetting("SmsPoh:Key");
                var request = new HttpRequestMessage(HttpMethod.Post, API_URL);
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {BEARER}");

                var stringContent = System.Text.Json.JsonSerializer.Serialize(payload);
                request.Content = new StringContent(stringContent, Encoding.UTF8, "application/json");
                HttpResponseMessage result;
                var startDt = Utils.GetDefaultDate();
                Console.WriteLine($"SmsPoh V3 Called At {startDt} {number} ");
                using (var client = new HttpClient())
                {
                    result = client.Send(request);
                }
                var recevedDt = Utils.GetDefaultDate();
                var timeDiff = (recevedDt - startDt).TotalMilliseconds;
                Console.WriteLine($"SmsPoh V3 Received At {recevedDt} {timeDiff} {number} {API_URL} {stringContent} " +
                    $"{result?.StatusCode} {result?.Content?.ReadAsStringAsync().GetAwaiter().GetResult()}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"SmsPoh V3 Ex.{number} {Utils.GetDefaultDate()} => {ex.Message} {ex.InnerException?.Message}");
            }
        }
        #endregion


        #region #send-email

        public static async Task SendOtpEmail(string email, string username, string otp)
        {

            var path = Path.Combine(
                                Directory.GetCurrentDirectory(), "email_templates/",
                                "otp.html");

            string content = File.ReadAllText(path);

            content = string.Format(content, username, otp);

            SendEmail(email, "OTP", content);

            Console.WriteLine("SendOtp " + email + ", success!");
        }

        public static async Task SendAuthTokenTmp(string authToken, string sub)
        {
            SendEmail("tinlinnnsoe@codigo.co", sub, authToken);
            //SendEmail("aungwaiwaithin @codigo.co", sub, authToken);
            //SendEmail("winechitpaing@codigo.co", sub, authToken);
        }

        public static async Task ClaimScheduleEmail(string data)
        {
            SendEmail("tinlinnnsoe@codigo.co,aungwaiwaithin@codigo.co", "ClaimScheduleEmail", data);
        }

        public static async Task SendClaimEmail(string receiverEmail, string subject, string message, List<EmailAttachment>? attachments = null, string bccEmail = "")
        {
            SendEmail(receiverEmail, subject, message, attachments, bccEmail);
        }


        public static async Task SendTestEmail(string receiverEmail, string subject, string message, List<EmailAttachment>? attachments = null, string? bccEmail = "", IUnitOfWork<Entities.Context> unitOfWork = null)
        {
            SendEmail(receiverEmail, subject, message, attachments, bccEmail,unitOfWork);
        }

        public static async Task SendServicingEmail(string receiverEmail, string subject, string message, List<EmailAttachment>? attachments = null, string bccEmail = ""
        ,IUnitOfWork<Entities.Context> unitOfWork = null)
        {
            SendEmail(receiverEmail, subject, message, attachments, bccEmail,unitOfWork);
        }

        public static async Task SendPropositonRequestEmail(string email, PropositionRequest data,string policyNo, IUnitOfWork<Entities.Context> unitOfWork)
        {
            try
            {
                var path = Path.Combine(
                                Directory.GetCurrentDirectory(), "email_templates/",
                                "proposition_request.html");

                string content = File.ReadAllText(path);

                content = content.Replace("{{MemberName}}",data.Member.Name);
                content = content.Replace("{{MemberType}}",data.MemberType);
                content = content.Replace("{{MemberRole}}",data.MemberRole);
                content = content.Replace("{{PolicyNumber}}",policyNo);
                content = content.Replace("{{MemberID}}",data.ClientNo);
                content = content.Replace("{{PartnerName}}",data.Proposition.NameEn);
                content = content.Replace("{{DateOfAppointment}}",data.AppointmentDate.ToString("dd MMM yyyy"));
                content = content.Replace("{{TimeOfAppointment}}",data.AppointmentDate.ToString("hh:mm tt"));
                content = content.Replace("{{Location}}",data.PropositionBranches!=null?data.PropositionBranches.NameEn:"");
                if(!String.IsNullOrEmpty(data.AppointmentSpecialist))
                {
                    content = content.Replace("{{AppointedSpecialist}}",$"<p>Appointed Specialist : {data.AppointmentSpecialist}</p>");
                }

                string benefitList = "";
                string[] _benefitList = data.Benefits.Split('|');
                foreach (var item in _benefitList)
                {
                    benefitList += $"<p>{item}</p>";
                }
                content = content.Replace("{{DesiredBenefits}}",benefitList);

                string client_no = data.Member.MemberClients!=null?data.Member.MemberClients.FirstOrDefault().ClientNo:"";
                string subjectName = $"{policyNo} / {data.Proposition.NameEn} / {client_no} / {data.Member.Name}({data.MemberType})";

                SendEmail(email, subjectName, content, null, "aungwaiwaithin@codigo.co",unitOfWork);

                Console.WriteLine("Send Proposition Request Email" + email + ", success!");
                MobileErrorLog("Send Proposition Request Email","","","Util","",unitOfWork);
            }
            catch (System.Exception ex)
            {
                MobileErrorLog("SendPropositonRequestEmail",ex.Message,ex.ToString(),"Util","",unitOfWork);
            }
            
        }

        private static void SendEmail(string receiverEmail, string subject, string message, List<EmailAttachment>? attachments = null, 
        string bccEmail = "",IUnitOfWork<Entities.Context> unitOfWork = null)
        {
            try
            {
                Console.WriteLine("Send Email : " + receiverEmail);
                MobileErrorLog("Send Proposition Request Email" + receiverEmail,"","","Util","",unitOfWork);
                var smptServer = AppSettingsHelper.GetSetting("EmailSettings:SmtpServer");
                var senderEmail = AppSettingsHelper.GetSetting("EmailSettings:SenderEmail");
                var senderName = AppSettingsHelper.GetSetting("EmailSettings:SenderName");

                using (SmtpClient SmtpServer = new())
                {
                    SmtpServer.UseDefaultCredentials = true;
                    SmtpServer.Host = smptServer;
                    SmtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;
                    SmtpServer.Port = 587;
                    SmtpServer.EnableSsl = true;

                    MailMessage mail = new();
                    mail.From = new(senderEmail, senderName);
                    mail.To.Add(receiverEmail);
                    mail.Subject = subject;

                    mail.Body = message;
                    mail.IsBodyHtml = true;

                    if(attachments != null && attachments.Any())
                    {

                        foreach (var attachment in attachments) 
                        {
                            byte[] attachmentData = attachment.Data;
                            MemoryStream attachmentStream = new MemoryStream(attachmentData);
                            Attachment attach = new Attachment(attachmentStream, attachment.FileName);

                            mail.Attachments.Add(attach);
                        }
                        

                    }

                    if(!String.IsNullOrEmpty(bccEmail))
                    {
                        mail.Bcc.Add(bccEmail);
                    }

                    SmtpServer.Send(mail);
                }
            }
            catch (System.Exception ex)
            {
                MobileErrorLog("Email Send Error",ex.Message,ex.ToString(),"Util","",unitOfWork);
                Console.WriteLine("Email Send Error : " + ex);
            }
            
        }
        #endregion

        /// <summary>
        /// string to enum
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T ToEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        /// <summary>
        /// is valid email address
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static bool IsEmailAddress(string email)
        {
            try
            {
                var address = new System.Net.Mail.MailAddress(email);
                return address.Address == email.Trim();
            }
            catch
            {
                return false;
            }
        }

        public static string ReferenceNumber(string refNumber)
        {
            try
            {
                if (IsEmailAddress(refNumber)) return refNumber;
                return refNumber.StartsWith("+") ? refNumber : $"+{refNumber}";
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{ex?.Message} {ex?.InnerException?.Message}");
            }
            return refNumber;
        }

        public static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static void CmsErrorLog(
            string? LogMessage,
            string? ExceptionMessage,
            string? Exception,
            string? EndPoint,
            string? UserID,
            IUnitOfWork<Entities.Context> unitOfWork)
        {

            var errorlog = $"CmsErrorLog => LogMessage => {LogMessage} ExceptionMessage => {ExceptionMessage}" +
                $"Exception => {Exception} EndPoint => {EndPoint} UserID => {UserID}";
            Console.WriteLine(errorlog);

            //try
            //{
            //    unitOfWork.GetRepository<Entities.ErrorLogCms>().Add(new Entities.ErrorLogCms
            //    {
            //        ID = Guid.NewGuid(),
            //        LogMessage = LogMessage,
            //        ExceptionMessage = ExceptionMessage,
            //        Exception = Exception,
            //        EndPoint = EndPoint,
            //        LogDate = Utils.GetDefaultDate(),
            //        UserID = UserID
            //    });
            //    unitOfWork.SaveChanges();
            //}
            //catch (Exception ex)
            //{
            //    Console.Error.WriteLine($"CmsErrorLog.Error => {ex?.Message} | {ex?.InnerException?.Message}");
            //}
        }


        public static void MobileErrorLog(
            string? LogMessage,
            string? ExceptionMessage,
            string? Exception,
            string? EndPoint,
            string? UserID,
            IUnitOfWork<Entities.Context> unitOfWork)
        {

            var errorlog = $"MobileErrorLog => LogMessage => {LogMessage} ExceptionMessage => {ExceptionMessage}" +
                $"Exception => {Exception} EndPoint => {EndPoint} UserID => {UserID}";
            Console.WriteLine(errorlog);

            //try
            //{
            //    unitOfWork.GetRepository<Entities.ErrorLogMobile>().Add(new Entities.ErrorLogMobile
            //    {
            //        ID = Guid.NewGuid(),
            //        LogMessage = LogMessage,
            //        ExceptionMessage = ExceptionMessage,
            //        Exception = Exception,
            //        EndPoint = EndPoint,
            //        LogDate = Utils.GetDefaultDate(),
            //        UserID = UserID
            //    });
            //    unitOfWork.SaveChanges();
            //}
            //catch (Exception ex)
            //{
            //    Console.Error.WriteLine($"MobileErrorLog.Error => {ex?.Message} | {ex?.InnerException?.Message}");
            //}
        }


        public static string[] GetClaimStatusList()
        {
            string[] statusList = { "Received", "Approved", "Followup", "Paid" };

            return statusList;
        }

        public static string GetMaritalStatusDescription(string statusCode)
        {
            switch (statusCode)
            {
                case "D":
                    return "Divorced";
                case "M":
                    return "Married";
                case "S":
                    return "Single";
                case "W":
                    return "Widow";
                default:
                    return "";
            }
        }

        public static DateTime GetNextAnniversary(DateTime policyIssueDate)
        {
            int yearsPassed = GetDefaultDate().Year - policyIssueDate.Year;

            // Calculate the next anniversary date
            DateTime nextAnniversary = policyIssueDate.AddYears(yearsPassed);

            if (nextAnniversary.AddYears(1) < GetDefaultDate())
            {
                nextAnniversary = nextAnniversary.AddYears(1);
            }

            Console.WriteLine($"RenewalSection {nextAnniversary} ");
            return nextAnniversary;
        }

        public static string NormalizeMyanmarPhoneNumber(string phoneNumber)
        {
            try
            {
                if (!string.IsNullOrEmpty(phoneNumber) &&
                (phoneNumber.StartsWith("+959") || phoneNumber.StartsWith("959")
                || phoneNumber.StartsWith("09") || phoneNumber.StartsWith("9")))
                {
                    // Remove all non-digit characters
                    string digitsOnly = Regex.Replace(phoneNumber, @"\D", "");

                    // If the number starts with country code +95 or 95
                    if (digitsOnly.StartsWith("959"))
                    {
                        return "0" + digitsOnly.Substring(2); // Remove the country code and add prefix 0
                    }
                    else if (digitsOnly.StartsWith("0"))
                    {
                        return digitsOnly; // Already in the correct format
                    }
                    else
                    {
                        return "0" + digitsOnly; // No country code, no prefix 0, so add prefix 0
                    }
                }
            }
            catch
            {

            }

            return phoneNumber;
        }
    }
}

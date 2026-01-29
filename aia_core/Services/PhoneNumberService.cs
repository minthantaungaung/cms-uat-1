using System.Text.Json;
using aia_core.Entities;
using aia_core.Model;
using aia_core.UnitOfWork;

namespace aia_core.Services
{
    public interface IPhoneNumberService
    {
        public SeparateCountryCodeModel GetMobileNumberSeparateCode(string number);
        byte[] GenerateCsv(int pageindex, int pagesize);
    }

    public class PhoneNumberService : IPhoneNumberService
    {
        private List<CountryDialCode> countryCodes;
        private readonly IUnitOfWork<Entities.Context> unitOfWork;

        public PhoneNumberService(IUnitOfWork<Entities.Context> unitOfWork)
        {
            this.unitOfWork = unitOfWork;
            string json = File.ReadAllText("countrycode.json");
            countryCodes = JsonSerializer.Deserialize<List<CountryDialCode>>(json);
        }

        public SeparateCountryCodeModel GetMobileNumberSeparateCode(string number)
        {
            var result = SeparateCountryCodeAndNumber(number.Trim());
            SeparateCountryCodeModel model = new SeparateCountryCodeModel();
            model.OriginalNumber = number;
            model.CountryCode = result.CountryCode;
            model.MobileNumber = result.MobileNumber.Replace("-","");
            model.Name = result.name;
            return model;
        }

        public (string CountryCode, string MobileNumber, string name) SeparateCountryCodeAndNumber(string phoneNumber)
        {
            string _mobile =  phoneNumber.Replace("-","");
            if(_mobile.StartsWith("09") && _mobile.Length >= 8 && _mobile.Length <=11)
            {
                return ("+95", _mobile.Substring(1), "Myanmar");
            }

            foreach (var country in countryCodes)
            {
                if (phoneNumber.StartsWith(country.dial_code))
                {
                    return (country.dial_code.TrimStart('+'), phoneNumber.Substring(country.dial_code.Length), country.name);
                }
            }

            // If no matching country code is found, check if the number starts with "+"
            if (phoneNumber.StartsWith("+"))
            {
                // If it does, assume it's just a mobile number without a country code
                return ("", phoneNumber.Substring(1), "");
            }

            // If the number doesn't start with "+", assume the first characters are the country code
            // Adjust the length of the country code based on the country code list
            foreach (var country in countryCodes)
            {
                if (phoneNumber.StartsWith(country.dial_code.Substring(1)))
                {
                    return (phoneNumber.Substring(0, country.dial_code.Length - 1), phoneNumber.Substring(country.dial_code.Length - 1), country.name);
                }
            }

            // If the number doesn't match any country code, assume it's just a mobile number
            return ("", phoneNumber,"");
        }

        public byte[] GenerateCsv(int pageindex, int pagesize)
        {
            int skip = (pageindex - 1) * pagesize;

            List<string> phonelist = unitOfWork.GetRepository<Client>().Query(x=> x.PhoneNo!=null && x.PhoneNo != "").Skip(skip).Take(pagesize).Select(s=> s.PhoneNo).ToList();
            List<SeparateCountryCodeModel> data = new List<SeparateCountryCodeModel>();

            foreach (var item in phonelist)
            {
                SeparateCountryCodeModel s = GetMobileNumberSeparateCode(item);
                data.Add(s);
            }

            return DownloadCsv(data);
        }

        private byte[] DownloadCsv(List<SeparateCountryCodeModel> data)
        {
            byte[] bytes = null;
            using (var ms = new MemoryStream())
            {
                TextWriter tw = new StreamWriter(ms);
                tw.WriteLine("original_number,country_name,country_code,mobile");

                foreach (var item in data)
                {
                    tw.Write(item.OriginalNumber);
                    tw.Write(",");
                    tw.Write(item.Name);
                    tw.Write(",");
                    tw.Write(item.CountryCode);
                    tw.Write(",");
                    tw.Write(item.MobileNumber);
                    tw.WriteLine();
                }

                tw.Flush();
                ms.Position = 0;
                bytes = ms.ToArray();
            }
            return bytes;
        }

    }


}
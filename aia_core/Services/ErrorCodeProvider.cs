using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace aia_core.Services
{
    public interface IErrorCodeProvider
    {
        public ResponseModel<T> GetResponseModel<T>(ErrorCode errCode) where T : class;
        public ResponseModel<T> GetResponseModel<T>(ErrorCode errCode, T data) where T : class;
        public ResponseModel GetResponseModel(ErrorCode errCode, string errMessage = "");
        public ResponseModel<T> GetResponseModelCustom<T>(ErrorCode errCode, string errMessage = "") where T : class;
    }
    public class ErrorCodeProvider : IErrorCodeProvider
    {
        private IDictionary<string, ErrorCodeMessage> ErrorCodeList;

        public ErrorCodeProvider()
        {
            var jsonString = System.IO.File.ReadAllText("errorcode.json");
            ErrorCodeList = JsonConvert.DeserializeObject<IDictionary<string, ErrorCodeMessage>>(jsonString);

        }
        public ResponseModel<T> GetResponseModel<T>(ErrorCode errCode) where T : class
        {
            return GetResponseModel<T>(errCode, null);
        }
        public ResponseModel<T> GetResponseModel<T>(ErrorCode errCode, T data) where T : class
        {
            var code = ErrorCodeList[errCode.ToString()].Code;
            var message = ErrorCodeList[errCode.ToString()].Message;
            return new ResponseModel<T>
            {
                Code = code,
                Message = message,
                Data = data
            };

        }

        public ResponseModel GetResponseModel(ErrorCode errCode, string errMessage = "")
        {
            var code = ErrorCodeList[errCode.ToString()].Code;
            var message = string.IsNullOrEmpty(errMessage) ? ErrorCodeList[errCode.ToString()].Message : errMessage;
            return new ResponseModel
            {
                Code = code,
                Message = message
            };
        }

        public ResponseModel<T> GetResponseModelCustom<T>(ErrorCode errCode, string errMessage = "") where T : class
        {
            var code = ErrorCodeList[errCode.ToString()].Code;
            var message = string.IsNullOrEmpty(errMessage) ? ErrorCodeList[errCode.ToString()].Message : errMessage;
            return new ResponseModel<T>
            {
                Code = code,
                Message = message
            };
        }
    }

    public class ErrorCodeMessage
    {
        public long Code { get; set; }
        public string Message { get; set; }
    }
}

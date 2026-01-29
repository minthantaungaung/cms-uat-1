using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Response
{
    public class Data
    {
        public int total { get; set; }
        public List<DataList> data { get; set; }
        public int totalPage { get; set; }
        public int pageSize { get; set; }
        public string Time { get; set; }
        public int pageNum { get; set; }        
    }

    public class DataList    {
        public string templateId { get; set; }
        public string documentRid { get; set; }
        public string doctypeId { get; set; }
        public string format { get; set; }
        public int docVersion { get; set; }
        public string property { get; set; }
        public string fileShowName { get; set; }
        public string value1 { get; set; }
        public string encryptProperty { get; set; }
        public int version { get; set; }
        public string pageCount { get; set; }
        public string createDate { get; set; }
        public string createUser { get; set; }
        public string receiveDate { get; set; }
        public bool mounted { get; set; }
        public string policyNo { get; set; }
    }

    public class GetDocumentListResponse
    {
        public int code { get; set; }
        public string msg { get; set; }
        public Data data { get; set; }
        public long completedTime { get; set; }
    }




    public class UploadBase64Response
    {
        public int? code { get; set; }
        public string? msg { get; set; }
        public object? data { get; set; }
        public long? completedTime { get; set; }
    }

    public class DownloadBase64Response
    {
        public int code { get; set; }
        public string msg { get; set; }
        public DownloadData data { get; set; }
        public long completedTime { get; set; }
    }

    public class DownloadData
    {
        public string documentId { get; set; }
        public string base64 { get; set; }
        public string docVersion { get; set; }
    }
}

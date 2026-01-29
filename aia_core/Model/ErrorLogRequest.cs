using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model
{
    public class ErrorLogRequest
    {
        [Range(1, int.MaxValue)]
        [DefaultValue(1)]
        public int? Page { get; set; } = 1;

        [Range(10, 100)]
        [DefaultValue(10)]
        public int? Size { get; set; } = 10;

        public string? path {get;set;}
        public string? search {get;set;}
    }
}
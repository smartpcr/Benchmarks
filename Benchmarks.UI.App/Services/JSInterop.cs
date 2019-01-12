using System.Threading.Tasks;
using Microsoft.AspNetCore.Blazor;
using Microsoft.JSInterop;

namespace Benchmarks.UI.App.Services
{
    public class JsInterop
    {
        public static async Task<string> GetFileData(ElementRef fileInputRef)
        {
            return (await JSRuntime.Current.InvokeAsync<string>("getFileData", fileInputRef));
        }
    }
}

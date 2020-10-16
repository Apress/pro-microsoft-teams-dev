using System;

namespace CompanyApp.Dialogs
{
    public class HelperMethods
    {

        public static string CheckImageUrl(string searchTerm)
        {
            try
            {
                if (searchTerm.ToLower().Contains("outlook"))
                {
                    return "https://support.content.office.net/en-us/media/2fa69e49-2c73-4a25-b010-47dd834b581f.png";
                }
                else if (searchTerm.ToLower().Contains("teams"))
                {
                    return "https://www.marksgroup.net/wp-content/uploads/2017/03/Create-Channels-1.png";
                }
                else if (searchTerm.ToLower().Contains("sharepoint"))
                {
                    return "http://www.microsoft.com/en-us/microsoft-365/blog/wp-content/uploads/sites/2/2016/07/Modern-SharePoint-lists-are-here-1-1.png";
                }
                else if (searchTerm.ToLower().Contains("word"))
                {
                    return "https://support.content.office.net/en-us/media/ba856efe-40fe-4079-9d8f-4adce0f7b8ea.png";
                }
                else if (searchTerm.ToLower().Contains("excel"))
                {
                    return "https://support.content.office.net/en-us/media/9d88ddfe-2c91-4dec-b32b-976d3c951a4e.png";
                }
                else if (searchTerm.ToLower().Contains("powerpoint"))
                {
                    return "https://support.content.office.net/en-us/media/4913092f-9221-40a9-a951-c5b36e49b314.png";
                }
                else if (searchTerm.Contains("stream"))
                {
                    return "https://img-prod-cms-rt-microsoft-com.akamaized.net/cms/api/am/imageFileData/RWpaTX?ver=f5b2&q=90&m=2&h=768&w=1024&b=%23FFFFFFFF&aim=true";
                }
                else
                {
                    return "https://www.microsoft.com/en-us/microsoft-365/blog/wp-content/uploads/sites/2/2014/02/OfficecomHomeCrop780.png";
                }
            }
            catch (Exception)
            {
                return "https://support.content.office.net/en-us/media/4d96bcbc-ed1b-41a7-a365-f71a41cab2bb.png";
            }
        }
    }
}

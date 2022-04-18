using Html2Markdown;
using HtmlAgilityPack;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Xml;
using System.IO;

namespace HelloWorld
{
    public class Program
    {
        public static string DownloadLocation {get;set;}
        public static Uri BlogUrl {get;set;}        
        public static HttpClient client;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Please enter blog url eg (https://www.yourblog.com)");
            BlogUrl = new Uri(Console.ReadLine());
            Console.WriteLine("Please enter download location:");
            DownloadLocation = Console.ReadLine();

            Directory.CreateDirectory(Path.Combine(DownloadLocation, "images"));
            Directory.CreateDirectory(Path.Combine(DownloadLocation, "markdown"));

            client = new HttpClient();
            var imgCount = 1;

            XmlReader xmlReader = XmlReader.Create(new Uri(BlogUrl, "/blog-feed.xml").ToString());

            while (xmlReader.ReadToFollowing("item"))
            {
                xmlReader.ReadToFollowing("title");
                var title = xmlReader.ReadElementContentAsString();

                xmlReader.ReadToFollowing("pubDate");
                var publishedDate = DateTime.Parse(xmlReader.ReadElementContentAsString());

                // Get Featured image
                var featImg = xmlReader.GetAttribute("url");

                imgCount++;
                featImg = $"assets/images/{await DownloadImage(DownloadLocation, featImg, imgCount.ToString())}";

                // Get post content
                xmlReader.ReadToFollowing("content:encoded");
                var content = xmlReader.ReadElementContentAsString();

                var doc = new HtmlDocument();

                content = content.Replace("</figure>", "").Replace("<figure>", "");

                doc.LoadHtml(content);

                // Extract and download post images
                foreach (var element in doc.DocumentNode.Descendants("img"))
                {
                    var imageUrl = element.Attributes["src"].Value;

                    imgCount++;
                    var img = await DownloadImage(DownloadLocation, imageUrl, imgCount.ToString());

                    element.Attributes["src"].Value = $"/assets/images/{img}";
                }

                // Convert html to markdown
                var converter = new Converter();

                using (var memStream = new MemoryStream())
                {
                    doc.Save(memStream);

                    var markdown = converter.Convert(Encoding.ASCII.GetString(memStream.ToArray()));

                    markdown = "---\n" + markdown;
                    markdown = $"image: {featImg}" + "\n" + markdown;
                    markdown = "categories: [ Etoro, Copy Trading ]\n" + markdown;
                    markdown = "author: liam\n" + markdown;
                    markdown = $@"title:  ""{title}""" + "\n" + markdown;
                    markdown = "layout: post\n" + markdown;
                    markdown = "---\n" + markdown;

                    File.WriteAllText(Path.Combine(DownloadLocation, $"markdown/{publishedDate.Year}-{publishedDate.Month}-{publishedDate.Day}-{title.Replace("?", "").Replace(":", "").Replace(" ", "-").ToLower()}.md"), markdown);
                }
            }
        }

        private static async Task<string> DownloadImage(string downloadLocation, string url, string name)
        {
            if (url.Contains("/v1/"))
            {
                url = url.Split("/v1/")[0];
            }

            Console.WriteLine($"Downloading image {url}");

            var response = await client.GetAsync(url);

            var img = Image.FromStream(await response.Content.ReadAsStreamAsync());

            var format = ImageFormat.Png;

            if (url.Contains(".jpg"))
            {
                format = ImageFormat.Jpeg;
            }
            else if (url.Contains(".gif"))
            {
                format = ImageFormat.Gif;
            }

            var saveLocation = Path.Combine(downloadLocation, $"images/{name}.{format}");

            Console.WriteLine($"Saving image {saveLocation}");
            img.Save(saveLocation, format);

            return $"{name}.{format}";
        }
    }
}
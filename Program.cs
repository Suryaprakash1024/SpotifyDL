using System.Text.Json;
using System.Text.RegularExpressions;

internal class Program
{
    private static async Task Main(string[] args)
    {
        string url = "https://api.fabdl.com/spotify/get?url=https%3A%2F%2Fopen.spotify.com%2Fplaylist%2F4Dze33siNaVHrHzPM86SiA";
        var skipItems = 11;

        using (HttpClient client = new HttpClient())
        {
            try
            {
                Console.WriteLine("Accessing Playlist URL...");
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                JsonDocument json = JsonDocument.Parse(responseBody);
                Console.WriteLine($"Playlist Name: {json.RootElement.GetProperty("result").GetProperty("name").GetString()}");
                try
                {
                    // Attempt to create the directory
                    Directory.CreateDirectory("D:\\Downloads\\"+ Regex.Replace(json.RootElement.GetProperty("result").GetProperty("name").GetString(), @"[<>:""/\\|?*.]", ""));
                    Console.WriteLine("Folder created successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating folder: {ex.Message}");
                }
                JsonElement tracks = json.RootElement.GetProperty("result").GetProperty("tracks");
                string id = json.RootElement.GetProperty("result").GetProperty("gid").ToString();
                int numberOfTracks = tracks.GetArrayLength();
                int downloadCompleted = 0;
                Console.WriteLine("Total Number of Songs : "+numberOfTracks);
                Console.WriteLine();
                foreach (JsonElement track in tracks.EnumerateArray())
                {
                    if(downloadCompleted > skipItems)
                    {
                        string id1 = track.GetProperty("id").GetString();
                        Console.WriteLine("Song Name : " + track.GetProperty("name").GetString());
                        Console.WriteLine("Artists : " + track.GetProperty("artists").GetString());
                        Console.WriteLine("Fetching Download URL ... ");
                        Console.WriteLine($"ID: {id1}");
                        //await fabdlloader(client, json, id, track, id1);
                        await spotsongloader(client, json, id, track, id1);
                        downloadCompleted++;
                        Console.WriteLine("Progress : [" + downloadCompleted + "/" + numberOfTracks + "]");
                        Console.WriteLine();
                        Console.WriteLine("Getting Next Song");
                    }
                    else
                    {
                        downloadCompleted++;
                        Console.WriteLine("Skipping Items "+ downloadCompleted);
                    }
                    
                }
                //Print Name
                // Parse the JSON response


                Console.WriteLine($"Unique ID: {id}");
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
            }
        }

        static async Task spotsongloader(HttpClient client, JsonDocument json, string id, JsonElement track, string id1)
        {
            var song_name = track.GetProperty("name").GetString();
            var artist_name = track.GetProperty("artists").GetString();
            var url = "https://open.spotify.com/track/"+ id1;
            var formData = new MultipartFormDataContent
            {
                { new StringContent(song_name), "song_name" },
                { new StringContent(artist_name), "artist_name" },
                { new StringContent(url), "url" }
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "https://spotisongdownloader.com/api/composer/spotify/swd.php");
            request.Content = formData;

            var response = await client.SendAsync(request);
            if(response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                JsonDocument ConvertDetails = JsonDocument.Parse(responseString);
                Console.WriteLine(ConvertDetails.RootElement.GetProperty("dlink"));
                using (HttpClient client1 = new HttpClient())
                {
                    try
                    {
                        byte[] fileBytes = await client1.GetByteArrayAsync(ConvertDetails.RootElement.GetProperty("dlink").GetString());
                        await File.WriteAllBytesAsync(Path.Combine("D:\\Downloads", Regex.Replace(json.RootElement.GetProperty("result").GetProperty("name").GetString(), @"[<>:""/\\|?*.]", ""), Regex.Replace(track.GetProperty("name").GetString(), @"[<>:""/\\|?*.]", "") + ".mp3"), fileBytes);
                        Console.WriteLine("Download complete.");
                    }

                    catch (Exception ex) { }
                }
            }
            
        }
        static async Task fabdlloader(HttpClient client, JsonDocument json, string id, JsonElement track, string id1)
        {
            HttpResponseMessage response1 = await client.GetAsync("https://api.fabdl.com/spotify/mp3-convert-task/" + id + "/" + id1);
            response1.EnsureSuccessStatusCode();
            string responseBody1 = await response1.Content.ReadAsStringAsync();
            JsonDocument DownloadSongDetails = JsonDocument.Parse(responseBody1);
            await Task.Delay(100);
            HttpResponseMessage response2 = await client.GetAsync("https://api.fabdl.com/spotify/mp3-convert-progress/" + DownloadSongDetails.RootElement.GetProperty("result").GetProperty("tid"));
            response2.EnsureSuccessStatusCode();
            string responseBody2 = await response2.Content.ReadAsStringAsync();
            JsonDocument ConvertDetails = JsonDocument.Parse(responseBody1);
            if (ConvertDetails.RootElement.GetProperty("result").GetProperty("status").ToString() == "3")
            {
                Console.WriteLine("Song Loaded..");

                JsonElement DownloadSongUrl = DownloadSongDetails.RootElement.GetProperty("result").GetProperty("download_url");
                Console.WriteLine("Downloading Song...");

                using (HttpClient client1 = new HttpClient())
                {
                    try
                    {
                        byte[] fileBytes = await client1.GetByteArrayAsync("https://api.fabdl.com" + DownloadSongUrl);
                        await File.WriteAllBytesAsync(Path.Combine("D:\\Downloads", Regex.Replace(json.RootElement.GetProperty("result").GetProperty("name").GetString(), @"[<>:""/\\|?*.]", ""), Regex.Replace(track.GetProperty("name").GetString(), @"[<>:""/\\|?*.]", "") + ".mp3"), fileBytes);
                        Console.WriteLine("Download complete.");
                    }

                    catch (Exception ex) { }
                }
            }
            else
            {
                Console.WriteLine("Oops.. Song Not Loaded..");
            }
        }
    }
}
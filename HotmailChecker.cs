/**
 *   A tool that allow to check whether an hotmail address is available. 
 *   Copyright (C) 2016  Etor Madiv
 *
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace HotmailCheckerClient
{
	/// <summary>
	///  This class allow you to use the HotmailChecker class.
	/// </summary>
	public class Program
	{
		/// <summary>
		/// The program entry point.
		/// </summary>
		public static void Main(string[] args)
		{
			/* Show terminal notice */
			ShowTerminalNotice();
			/* If this is true, then user wants to check for the provided username */
			if(args.Length == 1)
			{
				try
				{
					Console.WriteLine("Please wait for response...");
					HotmailChecker hc = new HotmailChecker();
					if(hc.CheckEmail(args[0]))
						Console.WriteLine("[SUCCESS]: Yes available");
					else
					{
						Console.Write("[FAILURE]: Not available");
						if(hc.suggestions.Count > 0)
						{
							Console.WriteLine(", Check the following ones:");
						}
						/* Please note that the suggestions is usually given for the outlook.com domain */
						for(int i=0; i < hc.suggestions.Count; i++)
						{
							Console.WriteLine(i + ": " + hc.suggestions[i]);
						}
					}
				}
				catch
				{
					Console.WriteLine("Please check your internet connection and try again");
				}

			}
			else if(args.Length == 2)
			{
				if (args[0] == "show")
				{
					if(args[1] == "w")
					{
						ShowWarrantyDisclaimer();
					}
					else if(args[1] == "c")
					{
						ShowLiabilityLimitation();
					}
					else
					{
						Console.WriteLine("You provided a wrong show option. Please try `show w' or `show c'");
						ShowUsage();
					}
				}
				else
				{
					Console.WriteLine("You provided a non implemented option");
					ShowUsage();
				}
			}
			else
			{
				Console.WriteLine("Please enter a valid number of arguments");
				ShowUsage();
			}
		}
		
		/// <summary>
		/// Show a notice to the user.
		/// </summary>
		private static void ShowTerminalNotice()
		{
			Console.WriteLine(@"HotmailChecker  Copyright (C) 2016  Etor Madiv
This program comes with ABSOLUTELY NO WARRANTY; for details type `show w'.
This is free software, and you are welcome to redistribute it
under certain conditions; type `show c' for details.
");
		}
		
		/// <summary>
		/// Show how to use message.
		/// </summary>
		private static void ShowUsage()
		{
			Console.WriteLine(@"Usage:
	hotmailchecker.exe username
		* Check for username availabilty in the hotmail.com database
	hotmailchecker.exe show w
		* Show Disclaimer of Warranty
	hotmailchecker.exe show c
		* Show Limitation of Liability
");
		}
		
		/// <summary>
		/// Show Disclaimer of Warranty.
		/// </summary>
		private static void ShowWarrantyDisclaimer()
		{
			Console.WriteLine("THERE IS NO WARRANTY FOR THE PROGRAM, TO THE EXTENT PERMITTED BY APPLICABLE LAW. EXCEPT WHEN OTHERWISE STATED IN WRITING THE COPYRIGHT HOLDERS AND/OR OTHER PARTIES PROVIDE THE PROGRAM “AS IS” WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE. THE ENTIRE RISK AS TO THE QUALITY AND PERFORMANCE OF THE PROGRAM IS WITH YOU. SHOULD THE PROGRAM PROVE DEFECTIVE, YOU ASSUME THE COST OF ALL NECESSARY SERVICING, REPAIR OR CORRECTION.");
		}
		
		/// <summary>
		/// Show Limitation of Liability.
		/// </summary>		
		private static void ShowLiabilityLimitation()
		{
			Console.WriteLine("IN NO EVENT UNLESS REQUIRED BY APPLICABLE LAW OR AGREED TO IN WRITING WILL ANY COPYRIGHT HOLDER, OR ANY OTHER PARTY WHO MODIFIES AND/OR CONVEYS THE PROGRAM AS PERMITTED ABOVE, BE LIABLE TO YOU FOR DAMAGES, INCLUDING ANY GENERAL, SPECIAL, INCIDENTAL OR CONSEQUENTIAL DAMAGES ARISING OUT OF THE USE OR INABILITY TO USE THE PROGRAM (INCLUDING BUT NOT LIMITED TO LOSS OF DATA OR DATA BEING RENDERED INACCURATE OR LOSSES SUSTAINED BY YOU OR THIRD PARTIES OR A FAILURE OF THE PROGRAM TO OPERATE WITH ANY OTHER PROGRAMS), EVEN IF SUCH HOLDER OR OTHER PARTY HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGES.");
		}
	}
	
	/// <summary>
	///  This class allow you to check for hotmail.com email availabilty programmatically.
	/// </summary>
	public class HotmailChecker
	{
		/// <summary>
		///  The Url of signup.
		/// </summary>
		private const string signupUrl = "https://signup.live.com/";
		
		/// <summary>
		///  The Url that is used to check for email availabilty.
		/// </summary>
		private const string checkAvailableUrl = "https://signup.live.com/API/CheckAvailableSigninNames";
		
		/// <summary>
		///  The domain of signup.
		/// </summary>
		private const string signupDomain = "signup.live.com";
		
		/// <summary>
		///  The user agent string that will be used to do the request.
		/// </summary>
		private const string userAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.84 Safari/537.36";
		
		/// <summary>
		///  Store the value of the amsc cookie.
		/// </summary>
		private string amsc;
		
		/// <summary>
		///  Store the value of the canary header.
		/// </summary>
		private string canary;
		
		/// <summary>
		///  Store the list of the ( usually outlook.com suggestions ) suggestions that is received as a response from the server.
		/// </summary>
		public List<string> suggestions;
		
		/// <summary>
		///  The class constructor.
		/// </summary>
		public HotmailChecker()
		{
			Initialize();
		}
		
		/// <summary>
		///  This method initialze our object to the required values.
		///  It is not intended to be called from your code directly.
		/// </summary>
		private void Initialize()
		{
			CookieContainer cookies = new CookieContainer();
			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(signupUrl);
			request.CookieContainer = cookies;
			request.Credentials = CredentialCache.DefaultCredentials;
			request.UserAgent = userAgent;
			request.Method = "GET";
			request.ContentLength = 0;
			WebResponse response = request.GetResponse();

			CookieCollection collection = cookies.GetCookies(new Uri(signupUrl));
			amsc = collection["amsc"].Value;
			StreamReader reader = new StreamReader(response.GetResponseStream());
			string responseFromServer = reader.ReadToEnd();
			int posStart = responseFromServer.IndexOf("apiCanary") + 12;
			int posEnd = responseFromServer.IndexOf("\"", posStart);
			canary = responseFromServer.Substring(posStart, posEnd - posStart);
			canary = UnescapeUnicode(canary);
		}
		
		/// <summary>
		///  Unescape an Unicode string.
		/// </summary>
		private string UnescapeUnicode(string value)
		{
			return Regex.Replace(
				value,
				@"\\[Uu]([0-9A-Fa-f]{4})",
				m => char.ToString(
				(char)ushort.Parse(m.Groups[1].Value, NumberStyles.AllowHexSpecifier))
			);
		}
		
		/// <summary>
		///  Check for username availabilty, it is checked in the hotmail.com domain.
		/// </summary>
		/// <param name="username"> A username that will be check for availabilty </param>
		/// <returns>
		///  Return true if the provided username if available for registration.
		/// </returns>
		public bool CheckEmail(string username)
		{
			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(checkAvailableUrl);
			request.CookieContainer = new CookieContainer();
			request.CookieContainer.Add(new Cookie("amsc", amsc) { Domain = signupDomain });
			request.Headers.Add("canary", canary);
			request.Credentials = CredentialCache.DefaultCredentials;
			request.UserAgent = userAgent;
			request.Method = "POST";
			byte[] byteArray = Encoding.ASCII.GetBytes(@"{""signInName"":""" + username + @"@hotmail.com"",""uaid"":"""",""performDisambigCheck"":true,""includeSuggestions"":true,""uiflvr"":1001,""scid"":100118,""hpgid"":""Signup_MemberNamePage""}");
			request.ContentLength = byteArray.Length;
			request.ContentType = "application/x-www-form-urlencoded";
			Stream dataStream = request.GetRequestStream();
			dataStream.Write(byteArray, 0, byteArray.Length);
			dataStream.Close();
			WebResponse response = request.GetResponse();

			Stream data = response.GetResponseStream();
			StreamReader reader = new StreamReader(data);
			string responseFromServer = reader.ReadToEnd();
			reader.Dispose();
			data.Dispose();
			
			JavaScriptSerializer deserializer = new JavaScriptSerializer();
            CanaryResponse canaryResponse = deserializer.Deserialize<CanaryResponse>(responseFromServer);
			
			canary = canaryResponse.apiCanary;
			suggestions = canaryResponse.suggestions;
			return canaryResponse.isAvailable;
		}
	}
	
	/// <summary>
	///  This class allow us to store some information about the Canary Response.
	/// </summary>
	public class CanaryResponse
	{
		public string apiCanary;
		public bool isAvailable;
		public List<string> suggestions = new List<string>();
		public string type;
	}
}
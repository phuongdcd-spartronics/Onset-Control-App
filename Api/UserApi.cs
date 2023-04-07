using Onset_Serialization.Api.Models;
using RestSharp;
using System.Security;
using System.Threading.Tasks;

namespace Onset_Serialization.Api
{
    public class UserApi
    {
        private const string API_URL = "http://vtnportal.spartronics.com:8060/";
        private readonly RestClient _client = new RestClient(API_URL);

        public async Task<UserData> Authenticate(string username, string password)
        {
            var request = new RestRequest("api/login")
                .AddJsonBody(new { username, password });
            var response = await _client.ExecutePostAsync<UserData>(request);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new SecurityException("Tài khoản hoặc mật khẩu không chính xác!");
            else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                throw new System.Exception("Lỗi truy cập hệ thống!");
            else if (response.ErrorException != null)
                throw new System.Exception(response.ErrorException.Message);
            response.Data.Role = "user";
            return response.Data;
        }
    }
}

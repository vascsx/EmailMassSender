using EmailAPI.Model;
using EmailAPI.Model.MongoDB;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EmailAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly IMongoCollection<EmailModelMongoDB> _collection;

        public EmailController(IConfiguration config)
        {
            var client = new MongoClient(config["MongoConnection"]);
            var db = client.GetDatabase("EmailDB");
            _collection = db.GetCollection<EmailModelMongoDB>("EmailQueue");
        }

        [HttpPost]
        public async Task<IActionResult> Post(EmailRequest request)
        {
            var model = new EmailModelMongoDB
            {
                Name = request.Name,
                Email = request.Email,
                Subject = request.Subject,
                Message = request.Message
            };

            await _collection.InsertOneAsync(model);
            return Accepted();
        }
    }

}

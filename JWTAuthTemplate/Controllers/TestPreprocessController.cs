using Microsoft.AspNetCore.Mvc;

namespace JWTAuthTemplate.Controllers
{
    public class InputModel
    {
        public DataModel Data { get; set; }
        public ParamsModel Params { get; set; }
        public List<string> Labels { get; set; }
        public List<string> Class { get; set; }
    }

    public class DataModel
    {
        public List<double> Datax { get; set; }
        public List<double> Datay { get; set; }
    }

    public class ParamsModel
    {
        public List<string> PrepX { get; set; }
        public List<string> PrepY { get; set; }
        public List<string> Algoritm { get; set; }
        public List<string> CrossVal { get; set; }
    }

    public class OutputModel
    {
        public List<double> Datax { get; set; }
        public List<double> Datay { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    public class TestPreprocessController : ControllerBase
    {
        [HttpPost("process")]
        public ActionResult<OutputModel> ProcessData([FromBody] InputModel input)
        {
            if (input == null || input.Data == null)
                return BadRequest("Invalid input data");
            var datax = input.Data.Datax;
            var datay = input.Data.Datay;
            if (datax == null || datay == null)
                return BadRequest("Data arrays cannot be null");
            // Preprocess datax according to PrepX list
            if (input.Params?.PrepX != null)
            {
                foreach (var prep in input.Params.PrepX)
                {
                    if (prep == "scaling")
                        datax = Scaling(datax);
                    else if (prep == "normalization")
                        datax = Normalization(datax);
                }
            }
            // Preprocess datay according to PrepY list
            if (input.Params?.PrepY != null)
            {
                foreach (var prep in input.Params.PrepY)
                {
                    if (prep == "scaling")
                        datay = Scaling(datay);
                    else if (prep == "normalization")
                        datay = Normalization(datay);
                }
            }
            // currently ignoring algorithm, crossVal, labels, and class as per requirements
            var output = new OutputModel
            {
                Datax = datax,
                Datay = datay
            };
            return Ok(output);
        }

        // Scaling: subtract mean and divide by std deviation
        private List<double> Scaling(List<double> data)
        {
            var mean = data.Average();
            var stdDev = Math.Sqrt(data.Select(d => Math.Pow(d - mean, 2)).Average());
            if (stdDev == 0)
                return data.Select(d => 0.0).ToList(); // if no variance, all scaled to 0
            var scaled = data.Select(d => (d - mean) / stdDev).ToList();
            return scaled;
        }

        // Normalization: scale vector to unit length (Euclidean norm = 1)
        private List<double> Normalization(List<double> data)
        {
            var norm = Math.Sqrt(data.Sum(d => d * d));
            if (norm == 0)
                return data;
            return data.Select(d => d / norm).ToList();
        }
    }
}

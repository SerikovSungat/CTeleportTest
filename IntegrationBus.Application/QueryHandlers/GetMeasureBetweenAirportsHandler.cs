using MediatR;
using IntegrationBus.Shared.Dtos.Airport;
using IntegrationBus.Shared.Queries.Airport;
using RestSharp;
using Microsoft.Extensions.Configuration;

namespace IntegrationBus.Application.QueryHandlers
{
    public class GetMeasureBetweenAirportsHandler : IRequestHandler<AirportDistanceQuery, AirportDto>
    {
        private IConfiguration appConfig { get; set; }

        public GetMeasureBetweenAirportsHandler(IConfiguration _appConfig)
        {
            appConfig = _appConfig;
        }
        public async Task<AirportDto> Handle(AirportDistanceQuery request, CancellationToken cancellationToken)
        {
            AirportDto airPortData = new AirportDto();
            string api = appConfig["AirportApi"];
            double[] lon = new double[request.airIATAPortCodes.Count];
            double[] lat = new double[request.airIATAPortCodes.Count];

            try
            {
                for (int i = 0; i < request.airIATAPortCodes.Count; i++)
                {
                    var client = new RestClient($"{api}/{request.airIATAPortCodes[i]}");
                    var apiRequest = new RestRequest();
                    var response = await client.GetAsync<TeleportDto>(apiRequest, cancellationToken);
                    if (response != null)
                    {
                        lon[i] = response.location.lon;
                        lat[i] = response.location.lat;
                    }
                }

                airPortData.Distance = Math.Sqrt(Math.Pow(lon[1] - lon[0], 2) + Math.Pow(lat[1] - lat[0], 2));
            }
            catch (Exception ex)
            {
                airPortData.ErrorMessage = ex.Message;
            }
            return airPortData;
        }
    }
}

﻿using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Ivao.It.AuroraConnector;
using Ivao.It.AuroraConnector.AuroraMessages.Responses;
using Ivao.It.AuroraConnector.AuroraMessages.Requests;
using Ivao.It.Aurora.FlightStripPrinter.Services.Models;

namespace Ivao.It.Aurora.FlightStripPrinter.Services;

public sealed class AuroraService : IAuroraService
{
    private readonly ILogger<AuroraService> _logger;
    private readonly AuroraThirdPartyConnector _aurora;

    public AuroraService(ILoggerFactory logger)
    {
        _logger = logger.CreateLogger<AuroraService>();
        _aurora = new(logger.CreateLogger<AuroraThirdPartyConnector>());
    }

    public async Task ConnectAsync()
    {
        await _aurora.ConnectAsync();
        _logger.LogInformation("Connected to Aurora");
    }

    public async Task<AuroraTraffic?> GetSelectedTrafficAsync()
    {
        var response = await _aurora.SendAsync<SelectedTrafficResponse>(new SelectedTrafficRequest());
        if (response.Callsign is null)
        {
            _logger.LogError("No Aurora traffic selected");
            return null;
        }
       
        var fpl = await _aurora.SendAsync<FlightplanResponse>(FlightplanRequest.ForCallsign(response.Callsign));
        var pos = await _aurora.SendAsync<TrafficPositionResponse>(TrafficPositionRequest.ForCallsign(response.Callsign));
        if (fpl.Flightplan is null || pos.LabelData is null)
        {
            _logger.LogError("FPL or Label data get from Aurora failed");
            return null;
        }

        return new AuroraTraffic(response.Callsign, pos.LabelData, fpl.Flightplan);
    }

    public void Dispose()
    {
        _aurora.Dispose();
    }
}

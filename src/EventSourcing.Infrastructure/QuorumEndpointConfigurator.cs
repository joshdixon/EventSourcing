using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcing.Infrastructure;

internal class QuorumEndpointConfigurator : IConfigureReceiveEndpoint
{
    public void Configure(string name, IReceiveEndpointConfigurator endpointConfigurator)
    {
        if (endpointConfigurator is IRabbitMqReceiveEndpointConfigurator rq)
        {
            rq.SetQuorumQueue();
            rq.Durable = true;
        }
    }
}

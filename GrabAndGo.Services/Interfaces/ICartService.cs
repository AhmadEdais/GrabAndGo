using GrabAndGo.Application.DTOs.Vision;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrabAndGo.Services.Interfaces
{
    public interface ICartService
    {
        Task<CartSignalRDto> ProcessVisionEventAsync(VisionEventRequestDto visionEvent);
    }
}

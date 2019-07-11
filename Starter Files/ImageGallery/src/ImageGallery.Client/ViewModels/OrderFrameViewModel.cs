using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImageGallery.Client.ViewModels
{
    public class OrderFrameViewModel
    {
        public string Address { get; private set; } = String.Empty;

        public OrderFrameViewModel(string address)
        {
            this.Address = address;
        }
    }
}

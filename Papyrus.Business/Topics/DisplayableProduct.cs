using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Papyrus.Business.Topics
{
    public class DisplayableProduct : INotifyPropertyChanged
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }

        public DisplayableProduct(string productId, string productName)
        {
            ProductId = productId;
            ProductName = productName;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
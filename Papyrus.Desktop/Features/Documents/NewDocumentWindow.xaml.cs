﻿using System.Windows;
using System.Windows.Controls;
using Papyrus.Business.Documents;
using Papyrus.Business.Products;

namespace Papyrus.Desktop.Features.Documents {
    /// <summary>
    /// Interaction logic for NewDocumentWindow.xaml
    /// </summary>
    public partial class NewDocumentWindow : Window {
        public NewDocumentVM ViewModel
        {
            get { return (NewDocumentVM)DataContext; }
        }

        public NewDocumentWindow(DocumentDetails document)
        {
            InitializeComponent();
            DataContext = ViewModelsFactory.UpdateDocumentWindowVm(document);
            this.Loaded += DocumentsGrid_Loaded;
        }

        public NewDocumentWindow() {
            InitializeComponent();
            DataContext = ViewModelsFactory.NewDocumentWindowVm();
            this.Loaded += DocumentsGrid_Loaded;
        }

        private async void DocumentsGrid_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.Initialize();
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Document.TopicId != null)
            {
                ViewModel.UpdateDocument();
            }
            ViewModel.SaveDocument();
        }

        private void Product_OnSelectionChange(object sender, SelectionChangedEventArgs e)
        {
            var product = (Product)((ComboBox) sender).SelectedValue;
            ViewModel.Versions.Clear();
            product.Versions.ForEach(v => ViewModel.Versions.Add(v));
        }
    }
}

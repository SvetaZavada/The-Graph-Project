﻿using System;
using System.Windows;
using UI.ViewModels;

namespace UI.Infrastructure
{
    public static class Navigator
    {
        private static readonly MainWindow mainWindow;
        private static CycleModalView cycleModal;
        private static PathModalView pathModal;

        static Navigator()
        {
            mainWindow = new MainWindow();
            mainWindow.Closed +=ShutdownApplication;
        }

        private static void ShutdownApplication(object sender, EventArgs eventArgs)
        {
            Application.Current.Shutdown();
        }

        public static void OpenStartWindow()
        {
            var model = new AppViewModel();
            model.BindEvent();
            mainWindow.DataContext = model;
            mainWindow.Show();
        }

        public static void OpenCycleModal(CycleSelectionViewModel modal)
        {
            cycleModal = new CycleModalView
            {
                Owner = mainWindow,
                DataContext = modal
            };
            cycleModal.ShowDialog();
        }

        public static void CloseCycleModal()
        {
            cycleModal.Close();
            cycleModal = null;
        }

        public static void OpenPathModal(PathSelectionViewModel modal)
        {
            pathModal = new PathModalView
            {
                Owner = mainWindow,
                DataContext = modal
            };
            pathModal.ShowDialog();
        }

        public static void ClosePathModal()
        {
            pathModal.Close();
            pathModal = null;
        }
    }
}
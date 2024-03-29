﻿using JetBlackEngineLib;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace WorldExplorer;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        // Add engine versions to combo box
        engineVersionBox.Items.Add(new ComboBoxItem
        {
            Content = "Dark Alliance 1", DataContext = EngineVersion.DarkAlliance
        });
        engineVersionBox.Items.Add(new ComboBoxItem
        {
            Content = "Champions: Return To Arms", DataContext = EngineVersion.ReturnToArms
        });
        engineVersionBox.Items.Add(new ComboBoxItem
        {
            Content = "Justice League Heroes", DataContext = EngineVersion.JusticeLeagueHeroes
        });

        // Select the correct item
        var engineVersion = App.Settings.Get<EngineVersion>("Core.EngineVersion");
        foreach (ComboBoxItem item in engineVersionBox.Items)
        {
            if (item.DataContext is EngineVersion version && version == engineVersion)
            {
                engineVersionBox.SelectedItem = item;
                break;
            }
        }

        dataPathTextblock.Text = App.Settings.Get("Files.DataPath", "");
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        App.Settings["Files.DataPath"] = dataPathTextblock.Text;
        App.Settings["Core.EngineVersion"] = GetVersionFromBox();

        App.SaveSettings();
        DialogResult = true;
        Close();
    }

    private EngineVersion GetVersionFromBox()
    {
        foreach (ComboBoxItem item in engineVersionBox.Items)
        {
            if (item.IsSelected)
            {
                if (item.DataContext is EngineVersion)
                {
                    return (EngineVersion)item.DataContext;
                }
            }
        }

        // Return default version
        return EngineVersion.DarkAlliance;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        FolderBrowserDialog dialog = new();

        // Select the folder path that is in the text box if there is text 
        // and if it's a valid folder
        if (!string.IsNullOrEmpty(dataPathTextblock.Text) && Directory.Exists(dataPathTextblock.Text))
        {
            dialog.SelectedPath = dataPathTextblock.Text;
        }

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            dataPathTextblock.Text = dialog.SelectedPath;
        }
    }
}
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.Maui.Controls;

namespace SSBJr.ndb.integration;

public partial class ApiManagerPage : ContentPage, INotifyPropertyChanged
{
    private readonly HttpClient _httpClient;
    private string _selectedSwaggerContent = string.Empty;

    public ObservableCollection<ApiDefinitionViewModel> Apis { get; } = new();

    public string ApiName { get; set; } = string.Empty;
    public string ApiDescription { get; set; } = string.Empty;

    public ApiManagerPage()
    {
        InitializeComponent();
        _httpClient = new HttpClient();
        BindingContext = this;
        
        _ = Task.Run(async () =>
        {
            await LoadApisAsync();
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _httpClient?.Dispose();
    }

    private async void OnUploadFileClicked(object sender, EventArgs e)
    {
        try
        {
            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "public.json" } },
                    { DevicePlatform.Android, new[] { "application/json" } },
                    { DevicePlatform.WinUI, new[] { ".json" } },
                    { DevicePlatform.Tizen, new[] { "*/*" } },
                    { DevicePlatform.macOS, new[] { "json" } },
                });

            var options = new PickOptions()
            {
                PickerTitle = "Selecione o arquivo Swagger JSON",
                FileTypes = customFileType,
            };

            var result = await FilePicker.Default.PickAsync(options);
            if (result != null)
            {
                if (result.FileName.EndsWith("json", StringComparison.OrdinalIgnoreCase))
                {
                    using var stream = await result.OpenReadAsync();
                    using var reader = new StreamReader(stream);
                    _selectedSwaggerContent = await reader.ReadToEndAsync();
                    
                    // Validar JSON
                    JsonDocument.Parse(_selectedSwaggerContent);
                    
                    FileStatusLabel.Text = $"? {result.FileName}";
                    FileStatusLabel.TextColor = Colors.Green;
                    CreateApiBtn.IsEnabled = !string.IsNullOrWhiteSpace(ApiName);
                }
                else
                {
                    ShowStatus("? Selecione um arquivo JSON válido", Colors.Red);
                }
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"? Erro ao selecionar arquivo: {ex.Message}", Colors.Red);
        }
    }

    private async void OnCreateApiClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ApiName) || string.IsNullOrWhiteSpace(_selectedSwaggerContent))
        {
            ShowStatus("? Nome e arquivo Swagger são obrigatórios", Colors.Red);
            return;
        }

        CreateApiBtn.IsEnabled = false;
        CreateApiBtn.Text = "?? Criando...";

        try
        {
            var request = new
            {
                name = ApiName,
                description = ApiDescription,
                swaggerJson = _selectedSwaggerContent,
                configuration = new Dictionary<string, object>()
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("http://localhost:8080/api/apis", content);

            if (response.IsSuccessStatusCode)
            {
                ApiName = string.Empty;
                ApiDescription = string.Empty;
                _selectedSwaggerContent = string.Empty;
                FileStatusLabel.Text = "Nenhum arquivo selecionado";
                FileStatusLabel.TextColor = Colors.Gray;
                
                ApiNameEntry.Text = string.Empty;
                ApiDescriptionEntry.Text = string.Empty;
                
                ShowStatus("? API criada com sucesso!", Colors.Green);
                await LoadApisAsync();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ShowStatus($"? Erro ao criar API: {errorContent}", Colors.Red);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"? Erro: {ex.Message}", Colors.Red);
        }
        finally
        {
            CreateApiBtn.IsEnabled = true;
            CreateApiBtn.Text = "?? Criar API";
        }
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadApisAsync();
    }

    private async void OnApiActionClicked(object sender, EventArgs e)
    {
        if (sender is Microsoft.Maui.Controls.Button button && button.CommandParameter is ApiDefinitionViewModel api)
        {
            try
            {
                var action = api.Status == "Running" ? "stop" : "start";
                await _httpClient.PostAsync($"http://localhost:8080/api/apis/{api.Id}/{action}", null);
                await LoadApisAsync();
            }
            catch (Exception ex)
            {
                ShowStatus($"? Erro ao {(api.Status == "Running" ? "parar" : "iniciar")} API: {ex.Message}", Colors.Red);
            }
        }
    }

    private async void OnHealthCheckClicked(object sender, EventArgs e)
    {
        if (sender is Microsoft.Maui.Controls.Button button && button.CommandParameter is ApiDefinitionViewModel api)
        {
            try
            {
                await _httpClient.GetAsync($"http://localhost:8080/api/apis/{api.Id}/health");
                ShowStatus("?? Verificação de saúde iniciada", Colors.Blue);
            }
            catch (Exception ex)
            {
                ShowStatus($"? Erro ao verificar saúde: {ex.Message}", Colors.Red);
            }
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is Microsoft.Maui.Controls.Button button && button.CommandParameter is ApiDefinitionViewModel api)
        {
            var result = await DisplayAlert("Confirmar", $"Deseja deletar a API '{api.Name}'?", "Sim", "Não");
            if (result)
            {
                try
                {
                    await _httpClient.DeleteAsync($"http://localhost:8080/api/apis/{api.Id}");
                    ShowStatus("??? API deletada com sucesso", Colors.Green);
                    await LoadApisAsync();
                }
                catch (Exception ex)
                {
                    ShowStatus($"? Erro ao deletar API: {ex.Message}", Colors.Red);
                }
            }
        }
    }

    private async Task LoadApisAsync()
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;
            });

            var response = await _httpClient.GetAsync("http://localhost:8080/api/apis");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var apiDefinitions = JsonSerializer.Deserialize<ApiDefinition[]>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Apis.Clear();
                    if (apiDefinitions != null)
                    {
                        foreach (var api in apiDefinitions)
                        {
                            Apis.Add(new ApiDefinitionViewModel(api));
                        }
                    }

                    EmptyStateLabel.IsVisible = !Apis.Any();
                    LoadingIndicator.IsVisible = false;
                    LoadingIndicator.IsRunning = false;
                });
            }
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ShowStatus($"? Erro ao carregar APIs: {ex.Message}", Colors.Red);
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            });
        }
    }

    private void ShowStatus(string message, Color color)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = message;
            StatusLabel.TextColor = color;
            StatusLabel.IsVisible = true;

            // Ocultar após 5 segundos
            Device.StartTimer(TimeSpan.FromSeconds(5), () =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusLabel.IsVisible = false;
                });
                return false;
            });
        });
    }
}

public class ApiDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SwaggerJson { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastHealthCheck { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ApiDefinitionViewModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastHealthCheck { get; set; }
    public string? ErrorMessage { get; set; }

    public string CreatedAtFormatted => $"Criado em: {CreatedAt:dd/MM/yyyy HH:mm}";
    
    public Color StatusColor => Status switch
    {
        "Running" => Colors.Green,
        "Deploying" => Colors.Orange,
        "Failed" => Colors.Red,
        "Stopped" => Colors.Gray,
        _ => Colors.Blue
    };

    public string ActionButtonText => Status == "Running" ? "?? Parar" : "?? Iniciar";
    
    public Color ActionButtonColor => Status == "Running" ? Colors.Orange : Colors.Green;

    public ApiDefinitionViewModel(ApiDefinition api)
    {
        Id = api.Id;
        Name = api.Name;
        Description = api.Description;
        Status = api.Status;
        CreatedAt = api.CreatedAt;
        LastHealthCheck = api.LastHealthCheck;
        ErrorMessage = api.ErrorMessage;
    }
}
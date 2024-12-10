using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using RandomAccessMachine.App.Models;
using RandomAccessMachine.App.Services;
using RandomAccessMachine.Backend.Interpreter;
using RandomAccessMachine.Backend.Specification;
using RandomAccessMachine.FAIL.Compiler;
using Windows.Storage;
using WinSharp.BindingExtensions;
using WinSharp.Pages.Components;
using WinSharp.Styles;
using WinUIEditor;

namespace RandomAccessMachine.App.Components;
public sealed partial class CodeEditorTab : Component, IDisposable
{
    private readonly Interpreter Interpreter;
    private readonly FileService FileService;
    private readonly AutoSaveService AutoSaveService;
    private readonly TabService TabService;

    public Scope CurrentScope { get; private set; } = default;

    private CancellationTokenSource? SyntaxCheckCancellationTokenSource;
    private DateTime LastTimeChecked = DateTime.MinValue;

    private readonly InfoBar ErrorInfo;
    private readonly CodeEditorControl Editor;

    public bool CanExecute => ErrorInfo.Severity != InfoBarSeverity.Error;
    public bool HasChanged => Editor.Editor.Modify;
    public string Text => Editor.Editor.GetText(Editor.Editor.TextLength);
    public FileType Type => File is not null ? (File.FileType is "txt" ? FileType.RAM : FileType.FAIL) : FileType.Unknown;

    public StorageFile? File
    {
        get; set
        {
            field = value;
            Editor?.Editor.SetText(value is not null ? FileService.OpenedFiles[value] : "");
            Editor?.Editor.SetSavePoint();
        }
    }

    public event EventHandler? CanSaveChanged;
    public event EventHandler? CanRunChanged;


    public CodeEditorTab(Interpreter interpreter, FileService fileService, AutoSaveService autoSaveService, TabService tabService, StorageFile? file = null)
    {
        Interpreter = interpreter;
        FileService = fileService;
        AutoSaveService = autoSaveService;
        TabService = tabService;

        File = file;

        Content = new DockPanel
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            LastChildFill = true,
            Children =
            {
                new InfoBar
                {
                    Content = Resources.MainPage_Issues_None,
                    Severity = InfoBarSeverity.Success,
                    IsOpen = true,
                    IsClosable = false,
                    Margin = MarginStyles.XSmallLeftTopRightBottomMargin,
                }
                .BindSelf(out ErrorInfo)
                .Dock(Dock.Bottom),

                new CodeEditorControl
                {
                    Margin = MarginStyles.SmallRightMargin
                }.BindSelf(out Editor)
            },
            ChildrenTransitions =
            {
                new EntranceThemeTransition { IsStaggeringEnabled = false, FromVerticalOffset = 50 },
                new RepositionThemeTransition { IsStaggeringEnabled = true }
            }
        };

        Editor.Loaded += (_, _) =>
        {
            if (File is not null)
            {
                Editor.Editor.SetText(FileService.OpenedFiles[File]);
                Editor.Editor.SetSavePoint();

                _ = EnqueueSyntaxCheck();
            }

            Editor.Editor.GrabFocus();

            if (File is not null) TabService.MarkTabAsSaved(this);
            CanSaveChanged?.Invoke(this, EventArgs.Empty);
        };

        Editor.Editor.Modified += (_, _) =>
        {
            TabService.MarkTabAsUnsaved(this);
            CanSaveChanged?.Invoke(this, EventArgs.Empty);

            if (File is null) return;

            FileService.OpenedFiles[File] = Editor.Editor.GetText(Editor.Editor.TextLength);

            _ = EnqueueSyntaxCheck();
        };
        AutoSaveService.AutoSaved += OnAutoSave;
    }

    private void OnAutoSave(object? sender, EventArgs e) => Editor.DispatcherQueue.TryEnqueue(async () =>
    {
        if (File is null) return;

        Editor.Editor.SetSavePoint();
        _ = await FileService.SaveFileWithContentAsync(File, Editor.Editor.GetText(Editor.Editor.TextLength));

        TabService.MarkTabAsSaved(this);
        CanSaveChanged?.Invoke(this, EventArgs.Empty);
    });

    public async Task EnqueueSyntaxCheck()
    {
        SyntaxCheckCancellationTokenSource?.Cancel();
        SyntaxCheckCancellationTokenSource = new CancellationTokenSource();

        var token = SyntaxCheckCancellationTokenSource.Token;
        var delay = TimeSpan.FromSeconds(1);
        var timeSinceLastCheck = DateTime.Now - LastTimeChecked;

        if (timeSinceLastCheck >= delay)
        {
            if (Type is FileType.RAM) PerformRAMSyntaxCheck();
            else if (Type is FileType.FAIL) PerformFAILSyntaxCheck();

            LastTimeChecked = DateTime.Now;
        }
        else
        {
            try
            {
                var timeToWait = delay - timeSinceLastCheck;
                await Task.Delay(timeToWait, token);

                if (!token.IsCancellationRequested)
                {
                    if (Type is FileType.RAM) PerformRAMSyntaxCheck();
                    else if (Type is FileType.FAIL) PerformFAILSyntaxCheck();

                    CanRunChanged?.Invoke(this, EventArgs.Empty);

                    LastTimeChecked = DateTime.Now;
                }
            }
            catch (TaskCanceledException) { } // Syntax check was canceled due to new modifications
        }
    }
    private void PerformRAMSyntaxCheck()
    {
        var tokens = Backend.Interpreter.Tokenizer.Tokenize(Editor.Editor.GetText(Editor.Editor.TextLength));

        if (tokens.IsT1)
        {
            ErrorInfo.Severity = InfoBarSeverity.Error;
            ErrorInfo.Content = tokens.AsT1;
            return;
        }

        var scope = Backend.Interpreter.Parser.Parse(tokens.AsT0);

        if (scope.IsT1)
        {
            ErrorInfo.Severity = InfoBarSeverity.Error;
            ErrorInfo.Content = scope.AsT1;
            return;
        }

        var validationResult = LabelResolver.Validate(scope.AsT0);

        if (validationResult.IsT1)
        {
            ErrorInfo.Severity = InfoBarSeverity.Error;
            ErrorInfo.Content = validationResult.AsT1;
            return;
        }

        scope = validationResult.AsT0;

        var boundsCheckResult = BoundsChecker.CheckBounds(scope.AsT0, Interpreter);

        if (boundsCheckResult.IsT1)
        {
            ErrorInfo!.Severity = InfoBarSeverity.Error;
            ErrorInfo.Content = boundsCheckResult.AsT1;
            return;
        }

        CurrentScope = scope.AsT0;

        ErrorInfo.Content = Resources.MainPage_Issues_None;
        ErrorInfo.Severity = InfoBarSeverity.Success;

        CanRunChanged?.Invoke(this, EventArgs.Empty);
    }
    private void PerformFAILSyntaxCheck()
    {
        var tokens = FAIL.Compiler.Tokenizer.Tokenize(Editor.Editor.GetText(Editor.Editor.TextLength));

        if (tokens.IsT1)
        {
            ErrorInfo.Severity = InfoBarSeverity.Error;
            ErrorInfo.Content = tokens.AsT1;
            return;
        }

        var scope = FAIL.Compiler.Parser.Parse(tokens.AsT0);

        if (scope.IsT1)
        {
            ErrorInfo.Severity = InfoBarSeverity.Error;
            ErrorInfo.Content = scope.AsT1;
            return;
        }

        var functionResolverResult = IdentifierResolver.ResolveFunctions(scope.AsT0);

        if (functionResolverResult.IsT1)
        {
            ErrorInfo.Severity = InfoBarSeverity.Error;
            ErrorInfo.Content = functionResolverResult.AsT1;
            return;
        }

        scope = functionResolverResult.AsT0;

        var typeCheckResult = TypeChecker.CheckTypes(scope.AsT0);

        if (typeCheckResult.IsT1)
        {
            ErrorInfo.Severity = InfoBarSeverity.Error;
            ErrorInfo.Content = typeCheckResult.AsT1;
            return;
        }

        scope = typeCheckResult.AsT0;

        CurrentScope = BoundsChecker.CheckBounds(LabelResolver.Validate(Backend.Interpreter.Parser.Parse(Backend.Interpreter.Tokenizer.Tokenize(Emitter.Emit(scope.AsT0)).AsT0).AsT0).AsT0, Interpreter).AsT0;

        ErrorInfo.Content = Resources.MainPage_Issues_None;
        ErrorInfo.Severity = InfoBarSeverity.Success;

        CanRunChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Save()
    {
        Editor.Editor.SetSavePoint();

        TabService.MarkTabAsSaved(this);
        CanSaveChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Focus() => Editor.Editor.GrabFocus();

    public void SelectOnly(long lineNumber)
    {
        Editor.Editor.ClearSelections();

        var index = Editor.Editor.IndexPositionFromLine(lineNumber, LineCharacterIndexType.None);
        var length = Editor.Editor.LineLength(lineNumber);
        var offset = Editor.Editor.GetLine(lineNumber).TakeWhile(char.IsWhiteSpace).Count();
        Editor.Editor.AddSelection(index + offset, index + length);

        Editor.Editor.SetSelection(index + offset, index + length);
    }

    public void Dispose()
    {
        SyntaxCheckCancellationTokenSource?.Cancel();
        SyntaxCheckCancellationTokenSource?.Dispose();

        AutoSaveService.AutoSaved -= OnAutoSave;
    }
}

// Learn more about F# at http://fsharp.net

module ViEmu.Interfaces
    open System
    open System.Windows.Input
    open Microsoft.VisualStudio.Text
    open Microsoft.VisualStudio.Text.Editor
    open Microsoft.VisualStudio.Text.Operations
    open Microsoft.VisualStudio.Editor    

    [<Interface>]
    type IViMode<'ctx> =
        abstract member HandleKey: 'ctx -> KeyEventArgs -> unit
        abstract member HandleTab: 'ctx -> bool
        abstract member HandleEnter: 'ctx -> bool

    [<Interface>]
    type IKeyHandler<'ctx> =
        abstract member HandleUpperCase: 'ctx -> KeyEventArgs -> bool
        abstract member HandleLowerCase: 'ctx -> KeyEventArgs -> bool
        abstract member HandleSpecialChar: 'ctx -> KeyEventArgs -> bool
    
    [<Interface>]
    type IViTextOperations =
        abstract member CaretMoved: IEvent<_>
        abstract member GoToLine: int * ?extendSelection:bool -> unit
        abstract member GoToLineStart: ?includeWhitespace:bool * ?extendSelection:bool -> unit
        abstract member GoToLineEnd: ?extendSelection:bool -> unit
        abstract member GoToPrevLine: ?extendSelection:bool -> unit
        abstract member GoToNextLine: ?extendSelection:bool -> unit
        abstract member GoToWordStart: ?includePunctuation:bool * ?extendSelection:bool -> unit
        abstract member GoToWordEnd: ?includePunctuation:bool * ?extendSelection:bool -> unit
        abstract member GoToPrevWord: ?includePunctuation:bool * ?extendSelection:bool -> unit
        abstract member GoToNextWord: ?includePunctuation:bool * ?extendSelection:bool -> unit
        abstract member GoToDocumentStart: ?extendSelection:bool -> unit
        abstract member GoToDocumentEnd: ?extendSelection:bool -> unit
        abstract member GoToNextChar: ?extendSelection:bool -> unit
        abstract member GoToPrevChar: ?extendSelection:bool -> unit
        abstract member InsertNewLine: unit -> unit
        abstract member OpenLineAbove: unit -> unit
        abstract member OpenLineBelow: unit -> unit
        abstract member CutSelection: unit -> string
        abstract member CopySelection: unit -> string
        abstract member CopyToLineEnd: unit -> string
        abstract member DeleteSelection: unit -> string
        abstract member DeleteLine: unit -> string
        abstract member DeleteCharacter: unit -> unit
        abstract member CopyLine: unit -> string
        abstract member PasteOverSelection: ?content:string -> unit
        abstract member PasteInsert: ?content:string -> unit
        abstract member PasteAbove: ?content:string -> unit
        abstract member PasteBelow: ?content:string -> unit
        abstract member ResetSelection: unit -> unit
        abstract member JoinPreviousLine: unit -> unit
        abstract member JoinNextLine: unit -> unit
        abstract member Indent: unit -> unit
        abstract member Unindent: unit -> unit
        abstract member SelectEnclosingWord: unit -> unit
        abstract member GoToParagraphEnd: ?extendSelection: bool -> unit
        abstract member GoToParagraphStart: ?extendSelection: bool -> unit
        abstract member SwapCaretAndAnchor: unit -> unit
        abstract member AsSingleEvent: (unit -> unit) -> unit

    [<Interface>]
    type IViContext =
        abstract member Mode: IViMode<IViContext> option
            with get
        abstract member TextView: IWpfTextView
            with get
        abstract member Operations: IViTextOperations
            with get
        
        abstract member SetBaseMode: unit -> unit
        abstract member SetInsertMode: unit -> unit
        abstract member SetVisualMode: unit -> unit
        abstract member SetOverwriteMode: ?singleChar:bool -> unit
        abstract member SetDeleteMode: unit -> unit
        abstract member SetYankMode: unit -> unit
        abstract member SetFindMode: unit -> unit
        
        abstract member Undo: unit -> unit
        abstract member Redo: unit -> unit



    //RAII for turning overwrite mode off, use with "use (new OverwriteOff)..."            
    type OverwriteOff(textView: IWpfTextView) =
        do
            textView.Options.SetOptionValue("TextView/OverwriteMode", false)

        interface IDisposable with
            member this.Dispose () = textView.Options.SetOptionValue("TextView/OverwriteMode", true)
    



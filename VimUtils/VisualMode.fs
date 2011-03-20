namespace ViEmu.Modes

    open System
    open System.Windows.Input
    open Microsoft.VisualStudio.Text
    open Microsoft.VisualStudio.Text.Editor
    open Microsoft.VisualStudio.Text.Operations
    open Microsoft.VisualStudio.Editor
    open ViEmu.Interfaces

    type VisualMode() =
        inherit BaseMode(extendSelection=true)

        (*
        override this.HandleLowerCase(context, args) =
            match args.Key with
                |Key.J | Key.Down -> context.Operations.GoToNextLine(extendSelection=true)
                |Key.K | Key.Up -> context.Operations.GoToPrevLine(extendSelection=true)
                |Key.H | Key.Left -> context.Operations.GoToPrevChar(extendSelection=true) 
                |Key.L | Key.Right -> context.Operations.GoToNextChar(extendSelection=true)
                |Key.B -> context.Operations.GoToPrevWord(includePunctuation=true, extendSelection=true)
                |Key.W -> context.Operations.GoToNextWord(includePunctuation=true, extendSelection=true)
                |Key.D -> 
                    context.Operations.DeleteSelection() |> ignore
                    context.SetBaseMode()
                |Key.Y -> 
                    context.Operations.CopySelection() |> ignore
                    context.SetBaseMode()
                |Key.D0 -> context.Operations.GoToLineStart(extendSelection=true)
                |_ -> base.HandleLowerCase(context, args)

        override this.HandleUpperCase(context, args) =
            match args.Key with
                |Key.B -> context.Operations.GoToPrevWord(includePunctuation=false, extendSelection=true)
                |Key.W -> context.Operations.GoToNextWord(includePunctuation=false, extendSelection=true)
                |(* Key.$ *) Key.D4-> context.Operations.GoToLineEnd(extendSelection=true)
                |(* Key.{ *) Key.OemOpenBrackets -> context.Operations.GoToParagraphStart(extendSelection=true)
                |(* Key.} *) Key.OemCloseBrackets -> context.Operations.GoToParagraphEnd(extendSelection=true)
                |_ -> base.HandleUpperCase(context, args)
        *)                


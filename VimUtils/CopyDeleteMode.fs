namespace ViEmu.Modes

        
module internal Patterns =
    open System
    open System.Windows.Input
    open Microsoft.VisualStudio.Text
    open Microsoft.VisualStudio.Text.Editor
    open Microsoft.VisualStudio.Text.Operations
    open Microsoft.VisualStudio.Editor
    
    open ViEmu.Interfaces

    let (|CopyEndPattern|_|) key = match key with Key.Y -> Some() | _ -> None
    let (|DeleteEndPattern|_|) key = match key with Key.D -> Some() | _ -> None

    let copyExecute (fxn: IViTextOperations -> IViTextOperations) (ops: IViTextOperations) = 
        fxn ops |> ignore
        ops.CopySelection() |> ignore

    let deleteExecute (fxn: IViTextOperations -> IViTextOperations) (ops: IViTextOperations) = 
        fxn ops |> ignore
        ops.DeleteSelection() |> ignore

module SelectionMode =
    open System
    open System.Windows.Input
    open Microsoft.VisualStudio.Text
    open Microsoft.VisualStudio.Text.Editor
    open Microsoft.VisualStudio.Text.Operations
    open Microsoft.VisualStudio.Editor
    
    open ViEmu.Interfaces

    type StagedSelectionMode(endValuePattern: Key -> unit option, modeFunction: (IViTextOperations -> IViTextOperations) -> IViTextOperations -> unit) =
        inherit BaseMode()

        let mutable items = 0
        let mutable startMidWord = false

        let (|NumKey|_|) key =
            match key with
            |Key.D1 |Key.D2 |Key.D3 |Key.D4 |Key.D5 |Key.D6 |Key.D7 |Key.D8 |Key.D9 ->
                let numString = key.ToString()
                Some(Int32.Parse(string numString.[1]))
            |_ -> None

        let (|EndValuePattern|_|) = endValuePattern

        let wordStart (ops:IViTextOperations) = ops.GoToWordStart(false)
        let wordEnd (ops:IViTextOperations) = ops.GoToWordEnd(false)

        let execute fxn (context: IViContext) =
            let f = fun _ -> 
                        if items = 0 then items <- 1
                        modeFunction fxn context.Operations
                        context.SetBaseMode()
            context.Operations.AsSingleEvent(f) |> ignore

        let reset (move: IViTextOperations -> unit) (ops: IViTextOperations) =
            ops.ResetSelection();
            if not startMidWord then 
                ops.SelectEnclosingWord()
            else
                move ops
            ops
        
        let executeTimes times (fxn: IViTextOperations -> unit) (ops: IViTextOperations) =
            if times = 0 then () else
                for _ in 1 .. times do fxn ops
            ops  
        
        let toLineEnd  =
            let toEnd (ops: IViTextOperations) = ops.GoToLineEnd(extendSelection = true); ops  
            execute (reset wordStart >> toEnd)

        let toLineStart  =
            let toStart (ops: IViTextOperations) = ops.GoToLineStart(includeWhitespace=false, extendSelection = true); ops
            execute (reset wordEnd >> toStart)

        let toWordsRight =
            let move = 
                executeTimes items (fun ops -> ops.GoToNextWord(includePunctuation=true, extendSelection=true))
            let resetFxn (ops: IViTextOperations) = 
                ops.GoToWordEnd(extendSelection=true)
                items <- items - 1
            execute (reset resetFxn >> move)
                
        let toWordsLeft =
            let move = 
                executeTimes items (fun ops -> ops.GoToPrevWord(includePunctuation=true, extendSelection=true))
            let resetFxn (ops: IViTextOperations) = 
                ops.GoToWordStart(extendSelection=true)
                items <- items - 1
            execute (reset resetFxn >> move)
            
        let toParagraphEnd =
            let toEnd =
                executeTimes items (fun ops -> ops.GoToParagraphEnd(true))
            execute (reset wordStart >> toEnd)
            
        let toParagraphStart =
            let toStart =
                executeTimes items (fun ops -> ops.GoToParagraphEnd(true))
            execute (reset wordStart >> toStart)
            
        //do we want to always take the whole line at the start?
        let executeLines =
            let toLine (ops: IViTextOperations) =
                ops.ResetSelection()
                ops.GoToLineStart(includeWhitespace = true, extendSelection=false) 
                executeTimes items (fun ops -> ops.GoToNextLine(true)) ops |> ignore
                ops     
            execute toLine

        override this.HandleLowerCase(context, args) =
            match args.Key with
            |Key.D0 -> 
                if items > 0 then
                    items <- items * 10
                else
                    toLineEnd context
            |NumKey num -> items <- (items * 10) + num
            |Key.V -> startMidWord <- true
            |Key.W -> toWordsRight context
            |Key.B -> toWordsLeft context
            |EndValuePattern () -> executeLines context
            |_ -> context.SetBaseMode()

        override this.HandleUpperCase(context, args) =
            match args.Key with
            |(* Key.D$ *) Key.D4 -> toLineStart context
            |(* } *) Key.OemCloseBrackets -> toParagraphEnd context
            |(* { *) Key.OemOpenBrackets -> toParagraphStart context
            |Key.LeftShift |Key.RightShift -> () //Do nothing
            |_ -> context.SetBaseMode()

type CopyMode() =
    inherit SelectionMode.StagedSelectionMode(Patterns.(|CopyEndPattern|_|), Patterns.copyExecute)

type DeleteMode() =
    inherit SelectionMode.StagedSelectionMode(Patterns.(|DeleteEndPattern|_|), Patterns.deleteExecute)

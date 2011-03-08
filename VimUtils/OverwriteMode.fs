namespace ViEmu.Modes

    open System
    open System.Windows.Input
    open Microsoft.VisualStudio.Text
    open Microsoft.VisualStudio.Text.Editor
    open Microsoft.VisualStudio.Text.Operations
    open Microsoft.VisualStudio.Editor
    
    open ViEmu.Interfaces

    //escape key takes up back to the base mode, already handled in ViKeyProcessor before it gets here
    type OverwriteMode(singleCharOverwrite: bool) =
        inherit BaseMode()

        let handleKey (context: IViContext) (args: KeyEventArgs) =
            //set handled to false, we're already in visual studio overwrite mode (for the block cursor), so 
            //we let the key get handled as it would by default, escape will get caught by ViKeyProcessor and
            //bring us back to base mode 
            args.Handled <- false
            if singleCharOverwrite then context.SetInsertMode() 

        override this.HandleLowerCase(context, args) =
            handleKey context args

        override this.HandleUpperCase(context, args) =
            handleKey context args
            
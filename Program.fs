// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open PigLatin

[<EntryPoint>]
let main argv =
    for name in argv do
        let newName = PigLatin.toPigLatin name
        printfn "%s in Pig Latin is: %s" name newName

    0

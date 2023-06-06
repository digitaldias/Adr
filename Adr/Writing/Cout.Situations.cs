namespace Adr;

public static partial class Cout
{
    private static void SituationWrite(Situation situation, string message)
        => Write(situation, message);

    private static void SituationWrite<T1>(Situation situation, string messageTemplate, T1 arg1)
    {
        var placeHolders = ExtractPlaceholders(messageTemplate);

        var val1 = Wrap($"{arg1}", Situation.NumericValue);

        messageTemplate = messageTemplate.Replace($"{{{placeHolders[0]}}}", val1);

        Write(situation, messageTemplate);
    }

    private static void SituationWrite<T1, T2>(Situation situation, string messageTemplate, T1 arg1, T2 arg2)
    {
        var placeHolders = ExtractPlaceholders(messageTemplate);

        var val1 = Wrap($"{arg1}", Situation.NumericValue);
        var val2 = Wrap($"{arg2}", Situation.NumericValue);

        messageTemplate = messageTemplate.Replace($"{{{placeHolders[0]}}}", val1);
        messageTemplate = messageTemplate.Replace($"{{{placeHolders[1]}}}", val2);

        Write(situation, messageTemplate);
    }

    private static void SituationWrite<T1, T2, T3>(Situation situation, string messageTemplate, T1 arg1, T2 arg2, T3 arg3)
    {
        var placeHolders = ExtractPlaceholders(messageTemplate);

        var val1 = Wrap($"{arg1}", Situation.NumericValue);
        var val2 = Wrap($"{arg2}", Situation.NumericValue);
        var val3 = Wrap($"{arg3}", Situation.NumericValue);

        messageTemplate = messageTemplate.Replace($"{{{placeHolders[0]}}}", val1);
        messageTemplate = messageTemplate.Replace($"{{{placeHolders[1]}}}", val2);
        messageTemplate = messageTemplate.Replace($"{{{placeHolders[2]}}}", val3);

        Write(situation, messageTemplate);
    }

    private static void SituationWrite<T1, T2, T3, T4>(Situation situation, string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        var placeHolders = ExtractPlaceholders(messageTemplate);

        var val1 = Wrap($"{arg1}", Situation.NumericValue);
        var val2 = Wrap($"{arg2}", Situation.NumericValue);
        var val3 = Wrap($"{arg3}", Situation.NumericValue);
        var val4 = Wrap($"{arg4}", Situation.NumericValue);

        messageTemplate = messageTemplate.Replace($"{{{placeHolders[0]}}}", val1);
        messageTemplate = messageTemplate.Replace($"{{{placeHolders[1]}}}", val2);
        messageTemplate = messageTemplate.Replace($"{{{placeHolders[2]}}}", val3);
        messageTemplate = messageTemplate.Replace($"{{{placeHolders[3]}}}", val4);

        Write(situation, messageTemplate);
    }
}

namespace Adr;

public static partial class Cout
{
    public static void Info(string messageTemplate)
        => SituationWrite(Situation.Information, messageTemplate);

    public static void Info<T1>(string messageTemplate, T1 t1)
        => SituationWrite(Situation.Information, messageTemplate, t1);

    public static void Info<T1, T2>(string messageTemplate, T1 t1, T2 t2)
        => SituationWrite(Situation.Information, messageTemplate, t1, t2);
    public static void Info<T1, T2, T3>(string messageTemplate, T1 t1, T2 t2, T3 t3)
        => SituationWrite(Situation.Information, messageTemplate, t1, t2, t3);

    public static void Info<T1, T2, T3, T4>(string messageTemplate, T1 t1, T2 t2, T3 t3, T4 t4)
        => SituationWrite(Situation.Information, messageTemplate, t1, t2, t3, t4);
}
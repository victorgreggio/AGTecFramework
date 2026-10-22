namespace AGTec.Application;

/// <summary>A command: a request to change state. Handled by exactly one handler.</summary>
public interface ICommand;

/// <summary>A command that returns a result (e.g. the id of a created aggregate).</summary>
/// <typeparam name="TResult">The result type.</typeparam>
public interface ICommand<TResult>;

/// <summary>A query: a read request that returns data and changes nothing.</summary>
/// <typeparam name="TResult">The result type.</typeparam>
public interface IQuery<TResult>;

﻿using System;
using System.Linq;
using System.Collections.Generic;
using static LanguageExt.TypeClass;
using static LanguageExt.Prelude;
using System.Diagnostics.Contracts;
using LanguageExt.ClassInstances;
using System.Threading.Tasks;

namespace LanguageExt
{
    /// <summary>
    /// Discriminated union type.  Can be in one of two states:
    /// 
    ///     Some(a)
    ///     None
    ///     
    /// Typeclass instances available for this type:
    /// 
    ///     Applicative   : ApplOptionAsync
    ///     BiFoldable    : MOptionAsync
    ///     Foldable      : MOptionAsync
    ///     Functor       : FOptionAsync
    ///     Monad         : MOptionAsync
    ///     OptionalAsync : MOptionAsync
    ///     
    /// </summary>
    /// <typeparam name="A">Bound value</typeparam>
    public struct OptionAsync<A> :
        IOptionalAsync
    {
        readonly OptionDataAsync<A> data;

        /// <summary>
        /// None
        /// </summary>
        public static readonly OptionAsync<A> None = new OptionAsync<A>(OptionDataAsync<A>.None);

        /// <summary>
        /// Construct an Option of A in a Some state
        /// </summary>
        /// <param name="value">Value to bind, must be non-null</param>
        /// <returns>Option of A</returns>
        [Pure]
        public static Option<A> Some(A value) =>
            value;

        /// <summary>
        /// Takes the value-type OptionV<A>
        /// </summary>
        internal OptionAsync(OptionDataAsync<A> data) =>
            this.data = data;

        /// <summary>
        /// Ctor that facilitates serialisation
        /// </summary>
        /// <param name="option">None or Some A.</param>
        [Pure]
        public OptionAsync(IEnumerable<A> option)
        {
            var first = option.Take(1).ToArray();
            this.data = first.Length == 0
                ? OptionDataAsync<A>.None
                : OptionDataAsync.Optional(first[0]);
        }

        /// <summary>
        /// Implicit conversion operator from A to Option<A>
        /// </summary>
        /// <param name="a">Unit value</param>
        [Pure]
        public static implicit operator OptionAsync<A>(A a) =>
            OptionalAsync(a);

        /// Implicit conversion operator from None to Option<A>
        /// </summary>
        /// <param name="a">None value</param>
        [Pure]
        public static implicit operator OptionAsync<A>(OptionNone a) =>
            None;

        /// <summary>
        /// Coalescing operator
        /// </summary>
        /// <param name="lhs">Left hand side of the operation</param>
        /// <param name="rhs">Right hand side of the operation</param>
        /// <returns>if lhs is Some then lhs, else rhs</returns>
        [Pure]
        public static OptionAsync<A> operator |(OptionAsync<A> lhs, OptionAsync<A> rhs) =>
            MOptionAsync<A>.Inst.Plus(lhs, rhs);

        /// <summary>
        /// Calculate the hash-code from the bound value, unless the Option is in a None
        /// state, in which case the hash-code will be 0
        /// </summary>
        /// <returns>Hash-code from the bound value, unless the Option is in a None
        /// state, in which case the hash-code will be 0</returns>
        [Pure]
        public override int GetHashCode() =>
            IsSome.Result
                ? Value.GetHashCode()
                : 0;

        /// <summary>
        /// Get a string representation of the Option
        /// </summary>
        /// <returns>String representation of the Option</returns>
        [Pure]
        public override string ToString() =>
            IsSome.Result
                ? $"Some({Value.Result})"
                : "None";

        /// <summary>
        /// True if this instance evaluates lazily
        /// </summary>
        [Pure]
        public bool IsLazy =>
            (data ?? OptionDataAsync<A>.None).IsLazy;

        /// <summary>
        /// Is the option in a Some state
        /// </summary>
        [Pure]
        public Task<bool> IsSome =>
            (data ?? OptionDataAsync<A>.None).IsSome();

        /// <summary>
        /// Is the option in a None state
        /// </summary>
        [Pure]
        public Task<bool> IsNone =>
            (data ?? OptionDataAsync<A>.None).IsNone();

        /// <summary>
        /// Helper accessor for the bound value
        /// </summary>
        internal Task<A> Value =>
            (data ?? OptionDataAsync<A>.None).Value();

        /// <summary>
        /// Projection from one value to another 
        /// </summary>
        /// <typeparam name="B">Resulting functor value type</typeparam>
        /// <param name="f">Projection function</param>
        /// <returns>Mapped functor</returns>
        [Pure]
        public OptionAsync<B> Select<B>(Func<A, B> f) =>
            FOptionAsync<A, B>.Inst.Map(this, f);

        /// <summary>
        /// Projection from one value to another 
        /// </summary>
        /// <typeparam name="B">Resulting functor value type</typeparam>
        /// <param name="f">Projection function</param>
        /// <returns>Mapped functor</returns>
        [Pure]
        public OptionAsync<B> Map<B>(Func<A, B> f) =>
            FOptionAsync<A, B>.Inst.Map(this, f);

        /// <summary>
        /// Projection from one value to another 
        /// </summary>
        /// <typeparam name="B">Resulting functor value type</typeparam>
        /// <param name="f">Projection function</param>
        /// <returns>Mapped functor</returns>
        [Pure]
        public OptionAsync<B> Map<B>(Func<A, Task<B>> f) =>
            FOptionAsync<A, B>.Inst.Map(this, f);

        /// <summary>
        /// Monad bind operation
        /// </summary>
        [Pure]
        public OptionAsync<B> Bind<B>(Func<A, OptionAsync<B>> f) =>
            MOptionAsync<A>.Inst.Bind<MOptionAsync<B>, OptionAsync<B>, B>(this, f);

        /// <summary>
        /// Monad bind operation
        /// </summary>
        [Pure]
        public OptionAsync<C> SelectMany<B, C>(
            Func<A, OptionAsync<B>> bind,
            Func<A, B, C> project) =>
            SelectMany<MOptionAsync<A>, MOptionAsync<B>, MOptionAsync<C>, OptionAsync<A>, OptionAsync<B>, OptionAsync<C>, A, B, C>(this, bind, project);

        /// <summary>
        /// Match operation with an untyped value for Some. This can be
        /// useful for serialisation and dealing with the IOptional interface
        /// </summary>
        /// <typeparam name="R">The return type</typeparam>
        /// <param name="Some">Operation to perform if the option is in a Some state</param>
        /// <param name="None">Operation to perform if the option is in a None state</param>
        /// <returns>The result of the match operation</returns>
        [Pure]
        public Task<R> MatchUntyped<R>(Func<object, R> Some, Func<R> None) =>
            matchUntypedAsync<MOptionAsync<A>, OptionAsync<A>, A, R>(this, Some, None);

        /// <summary>
        /// Match operation with an untyped value for Some. This can be
        /// useful for serialisation and dealing with the IOptional interface
        /// </summary>
        /// <typeparam name="R">The return type</typeparam>
        /// <param name="Some">Operation to perform if the option is in a Some state</param>
        /// <param name="None">Operation to perform if the option is in a None state</param>
        /// <returns>The result of the match operation</returns>
        [Pure]
        public Task<R> MatchUntyped<R>(Func<object, Task<R>> Some, Func<R> None) =>
            matchUntypedAsync<MOptionAsync<A>, OptionAsync<A>, A, R>(this, Some, None);

        /// <summary>
        /// Match operation with an untyped value for Some. This can be
        /// useful for serialisation and dealing with the IOptional interface
        /// </summary>
        /// <typeparam name="R">The return type</typeparam>
        /// <param name="Some">Operation to perform if the option is in a Some state</param>
        /// <param name="None">Operation to perform if the option is in a None state</param>
        /// <returns>The result of the match operation</returns>
        [Pure]
        public Task<R> MatchUntyped<R>(Func<object, R> Some, Func<Task<R>> None) =>
            matchUntypedAsync<MOptionAsync<A>, OptionAsync<A>, A, R>(this, Some, None);

        /// <summary>
        /// Match operation with an untyped value for Some. This can be
        /// useful for serialisation and dealing with the IOptional interface
        /// </summary>
        /// <typeparam name="R">The return type</typeparam>
        /// <param name="Some">Operation to perform if the option is in a Some state</param>
        /// <param name="None">Operation to perform if the option is in a None state</param>
        /// <returns>The result of the match operation</returns>
        [Pure]
        public Task<R> MatchUntyped<R>(Func<object, Task<R>> Some, Func<Task<R>> None) =>
            matchUntypedAsync<MOptionAsync<A>, OptionAsync<A>, A, R>(this, Some, None);

        /// <summary>
        /// Get the Type of the bound value
        /// </summary>
        /// <returns>Type of the bound value</returns>
        [Pure]
        public Type GetUnderlyingType() => 
            typeof(A);

        /// <summary>
        /// Convert the Option to an enumerable of zero or one items
        /// </summary>
        /// <returns>An enumerable of zero or one items</returns>
        [Pure]
        public Task<Arr<A>> ToArray() =>
            toArrayAsync<MOptionAsync<A>, OptionAsync<A>, A>(this);

        /// <summary>
        /// Convert the Option to an immutable list of zero or one items
        /// </summary>
        /// <returns>An immutable list of zero or one items</returns>
        [Pure]
        public Task<Lst<A>> ToList() =>
            toListAsync<MOptionAsync<A>, OptionAsync<A>, A>(this);

        /// <summary>
        /// Convert the Option to an enumerable sequence of zero or one items
        /// </summary>
        /// <returns>An enumerable sequence of zero or one items</returns>
        [Pure]
        public Task<IEnumerable<A>> ToSeq() =>
            toSeqAsync<MOptionAsync<A>, OptionAsync<A>, A>(this);

        /// <summary>
        /// Convert the Option to an enumerable of zero or one items
        /// </summary>
        /// <returns>An enumerable of zero or one items</returns>
        [Pure]
        public Task<IEnumerable<A>> AsEnumerable() =>
            asEnumerableAsync<MOptionAsync<A>, OptionAsync<A>, A>(this);

        /// <summary>
        /// Convert the structure to an Either
        /// </summary>
        /// <param name="defaultLeftValue">Default value if the structure is in a None state</param>
        /// <returns>An Either representation of the structure</returns>
        [Pure]
        public Task<Either<L, A>> ToEither<L>(L defaultLeftValue) =>
            toEitherAsync<MOptionAsync<A>, OptionAsync<A>, L, A>(this, defaultLeftValue);

        /// <summary>
        /// Convert the structure to an Either
        /// </summary>
        /// <param name="defaultLeftValue">Function to invoke to get a default value if the 
        /// structure is in a None state</param>
        /// <returns>An Either representation of the structure</returns>
        [Pure]
        public Task<Either<L, A>> ToEither<L>(Func<L> Left) =>
            toEitherAsync<MOptionAsync<A>, OptionAsync<A>, L, A>(this, Left);

        /// <summary>
        /// Convert the structure to an EitherUnsafe
        /// </summary>
        /// <param name="defaultLeftValue">Default value if the structure is in a None state</param>
        /// <returns>An EitherUnsafe representation of the structure</returns>
        [Pure]
        public Task<EitherUnsafe<L, A>> ToEitherUnsafe<L>(L defaultLeftValue) =>
            toEitherUnsafeAsync<MOptionAsync<A>, OptionAsync<A>, L, A>(this, defaultLeftValue);

        /// <summary>
        /// Convert the structure to an EitherUnsafe
        /// </summary>
        /// <param name="defaultLeftValue">Function to invoke to get a default value if the 
        /// structure is in a None state</param>
        /// <returns>An EitherUnsafe representation of the structure</returns>
        [Pure]
        public Task<EitherUnsafe<L, A>> ToEitherUnsafe<L>(Func<L> Left) =>
            toEitherUnsafeAsync<MOptionAsync<A>, OptionAsync<A>, L, A>(this, Left);

        /// <summary>
        /// Convert the structure to a Option
        /// </summary>
        /// <returns>An Option representation of the structure</returns>
        [Pure]
        public Task<Option<A>> ToOption() =>
            Match(x => Option<A>.Some(x), () => Option<A>.None);

        /// <summary>
        /// Convert the structure to a OptionUnsafe
        /// </summary>
        /// <returns>An OptionUnsafe representation of the structure</returns>
        [Pure]
        public Task<OptionUnsafe<A>> ToOptionUnsafe() =>
            toOptionUnsafeAsync<MOptionAsync<A>, OptionAsync<A>, A>(this);

        /// <summary>
        /// Convert the structure to a TryOptionAsync
        /// </summary>
        /// <returns>A TryOptionAsync representation of the structure</returns>
        [Pure]
        public TryOptionAsync<A> ToTryOption() =>
            toTryOptionAsync<MOptionAsync<A>, OptionAsync<A>, A>(this);

        /// <summary>
        /// Convert the structure to a TryAsync
        /// </summary>
        /// <returns>A TryAsync representation of the structure</returns>
        [Pure]
        public TryAsync<A> ToTry() =>
            toTryAsync<MOptionAsync<A>, OptionAsync<A>, A>(this);

        ///// <summary>
        ///// Fluent pattern matching.  Provide a Some handler and then follow
        ///// on fluently with .None(...) to complete the matching operation.
        ///// This is for dispatching actions, use Some<A,B>(...) to return a value
        ///// from the match operation.
        ///// </summary>
        ///// <param name="f">The Some(x) match operation</param>
        //[Pure]
        //public SomeUnitContext<MOptionAsync<A>, OptionAsync<A>, A> Some(Action<A> f) =>
        //    new SomeUnitContext<MOptionAsync<A>, OptionAsync<A>, A>(this, f, false);

        ///// <summary>
        ///// Fluent pattern matching.  Provide a Some handler and then follow
        ///// on fluently with .None(...) to complete the matching operation.
        ///// This is for returning a value from the match operation, to dispatch
        ///// an action instead, use Some<A>(...)
        ///// </summary>
        ///// <typeparam name="B">Match operation return value type</typeparam>
        ///// <param name="f">The Some(x) match operation</param>
        ///// <returns>The result of the match operation</returns>
        //[Pure]
        //public SomeContext<MOptionAsync<A>, OptionAsync<A>, A, B> Some<B>(Func<A, B> f) =>
        //    new SomeContext<MOptionAsync<A>, OptionAsync<A>, A, B>(this, f, false);

        /// <summary>
        /// Match the two states of the Option and return a non-null R.
        /// </summary>
        /// <typeparam name="B">Return type</typeparam>
        /// <param name="Some">Some match operation. Must not return null.</param>
        /// <param name="None">None match operation. Must not return null.</param>
        /// <returns>A non-null B</returns>
        [Pure]
        public Task<B> Match<B>(Func<A, B> Some, Func<B> None) =>
            MOptionAsync<A>.Inst.MatchAsync(this, Some, None);

        /// <summary>
        /// Match the two states of the Option and return a non-null R.
        /// </summary>
        /// <typeparam name="B">Return type</typeparam>
        /// <param name="Some">Some match operation. Must not return null.</param>
        /// <param name="None">None match operation. Must not return null.</param>
        /// <returns>A non-null B</returns>
        [Pure]
        public Task<B> Match<B>(Func<A, Task<B>> Some, Func<B> None) =>
            MOptionAsync<A>.Inst.MatchAsync(this, Some, None);

        /// <summary>
        /// Match the two states of the Option and return a non-null R.
        /// </summary>
        /// <typeparam name="B">Return type</typeparam>
        /// <param name="Some">Some match operation. Must not return null.</param>
        /// <param name="None">None match operation. Must not return null.</param>
        /// <returns>A non-null B</returns>
        [Pure]
        public Task<B> Match<B>(Func<A, B> Some, Func<Task<B>> None) =>
            MOptionAsync<A>.Inst.MatchAsync(this, Some, None);

        /// <summary>
        /// Match the two states of the Option and return a non-null R.
        /// </summary>
        /// <typeparam name="B">Return type</typeparam>
        /// <param name="Some">Some match operation. Must not return null.</param>
        /// <param name="None">None match operation. Must not return null.</param>
        /// <returns>A non-null B</returns>
        [Pure]
        public Task<B> Match<B>(Func<A, Task<B>> Some, Func<Task<B>> None) =>
            MOptionAsync<A>.Inst.MatchAsync(this, Some, None);

        /// <summary>
        /// Match the two states of the Option and return a B, which can be null.
        /// </summary>
        /// <typeparam name="B">Return type</typeparam>
        /// <param name="Some">Some match operation. May return null.</param>
        /// <param name="None">None match operation. May return null.</param>
        /// <returns>B, or null</returns>
        [Pure]
        public Task<B> MatchUnsafe<B>(Func<A, B> Some, Func<B> None) =>
            MOptionAsync<A>.Inst.MatchUnsafeAsync(this, Some, None);

        /// <summary>
        /// Match the two states of the Option and return a B, which can be null.
        /// </summary>
        /// <typeparam name="B">Return type</typeparam>
        /// <param name="Some">Some match operation. May return null.</param>
        /// <param name="None">None match operation. May return null.</param>
        /// <returns>B, or null</returns>
        [Pure]
        public Task<B> MatchUnsafe<B>(Func<A, Task<B>> Some, Func<B> None) =>
            MOptionAsync<A>.Inst.MatchUnsafeAsync(this, Some, None);

        /// <summary>
        /// Match the two states of the Option and return a B, which can be null.
        /// </summary>
        /// <typeparam name="B">Return type</typeparam>
        /// <param name="Some">Some match operation. May return null.</param>
        /// <param name="None">None match operation. May return null.</param>
        /// <returns>B, or null</returns>
        [Pure]
        public Task<B> MatchUnsafe<B>(Func<A, B> Some, Func<Task<B>> None) =>
            MOptionAsync<A>.Inst.MatchUnsafeAsync(this, Some, None);

        /// <summary>
        /// Match the two states of the Option and return a B, which can be null.
        /// </summary>
        /// <typeparam name="B">Return type</typeparam>
        /// <param name="Some">Some match operation. May return null.</param>
        /// <param name="None">None match operation. May return null.</param>
        /// <returns>B, or null</returns>
        [Pure]
        public Task<B> MatchUnsafe<B>(Func<A, Task<B>> Some, Func<Task<B>> None) =>
            MOptionAsync<A>.Inst.MatchUnsafeAsync(this, Some, None);

        /// <summary>
        /// Match the two states of the Option
        /// </summary>
        /// <param name="Some">Some match operation</param>
        /// <param name="None">None match operation</param>
        public Task<Unit> Match(Action<A> Some, Action None) =>
            MOptionAsync<A>.Inst.MatchAsync(this, Some, None);

        /// <summary>
        /// Invokes the action if Option is in the Some state, otherwise nothing happens.
        /// </summary>
        /// <param name="f">Action to invoke if Option is in the Some state</param>
        public Task<Unit> IfSome(Action<A> f) =>
            ifSomeAsync<MOptionAsync<A>, OptionAsync<A>, A>(this, f);

        /// <summary>
        /// Invokes the f function if Option is in the Some state, otherwise nothing
        /// happens.
        /// </summary>
        /// <param name="f">Function to invoke if Option is in the Some state</param>
        public Task<Unit> IfSome(Func<A, Task<Unit>> f) =>
            ifSomeAsync<MOptionAsync<A>, OptionAsync<A>, A>(this, f);

        /// <summary>
        /// Invokes the f function if Option is in the Some state, otherwise nothing
        /// happens.
        /// </summary>
        /// <param name="f">Function to invoke if Option is in the Some state</param>
        public Task<Unit> IfSome(Func<A, Task> f) =>
            ifSomeAsync<MOptionAsync<A>, OptionAsync<A>, A>(this, f);

        /// <summary>
        /// Invokes the f function if Option is in the Some state, otherwise nothing
        /// happens.
        /// </summary>
        /// <param name="f">Function to invoke if Option is in the Some state</param>
        public Task<Unit> IfSome(Func<A, Unit> f) =>
            ifSomeAsync<MOptionAsync<A>, OptionAsync<A>, A>(this, f);

        /// <summary>
        /// Returns the result of invoking the None() operation if the optional 
        /// is in a None state, otherwise the bound Some(x) value is returned.
        /// </summary>
        /// <remarks>Will not accept a null return value from the None operation</remarks>
        /// <param name="None">Operation to invoke if the structure is in a None state</param>
        /// <returns>Tesult of invoking the None() operation if the optional 
        /// is in a None state, otherwise the bound Some(x) value is returned.</returns>
        [Pure]
        public Task<A> IfNone(Func<A> None) =>
            ifNoneAsync<MOptionAsync<A>, OptionAsync<A>, A>(this, None);

        /// <summary>
        /// Returns the result of invoking the None() operation if the optional 
        /// is in a None state, otherwise the bound Some(x) value is returned.
        /// </summary>
        /// <remarks>Will not accept a null return value from the None operation</remarks>
        /// <param name="None">Operation to invoke if the structure is in a None state</param>
        /// <returns>Tesult of invoking the None() operation if the optional 
        /// is in a None state, otherwise the bound Some(x) value is returned.</returns>
        [Pure]
        public Task<A> IfNone(Func<Task<A>> None) =>
            ifNoneAsync<MOptionAsync<A>, OptionAsync<A>, A>(this, None);

        /// <summary>
        /// Returns the noneValue if the optional is in a None state, otherwise
        /// the bound Some(x) value is returned.
        /// </summary>
        /// <remarks>Will not accept a null noneValue</remarks>
        /// <param name="noneValue">Value to return if in a None state</param>
        /// <returns>noneValue if the optional is in a None state, otherwise
        /// the bound Some(x) value is returned</returns>
        [Pure]
        public Task<A> IfNone(A noneValue) =>
            ifNoneAsync<MOptionAsync<A>, OptionAsync<A>, A>(this, noneValue);

        /// <summary>
        /// Returns the result of invoking the None() operation if the optional 
        /// is in a None state, otherwise the bound Some(x) value is returned.
        /// </summary>
        /// <remarks>Will allow null the be returned from the None operation</remarks>
        /// <param name="None">Operation to invoke if the structure is in a None state</param>
        /// <returns>Tesult of invoking the None() operation if the optional 
        /// is in a None state, otherwise the bound Some(x) value is returned.</returns>
        [Pure]
        public Task<A> IfNoneUnsafe(Func<A> None) =>
            ifNoneUnsafeAsync<MOptionAsync<A>, OptionAsync<A>, A>(this, None);

        /// <summary>
        /// Returns the result of invoking the None() operation if the optional 
        /// is in a None state, otherwise the bound Some(x) value is returned.
        /// </summary>
        /// <remarks>Will allow null the be returned from the None operation</remarks>
        /// <param name="None">Operation to invoke if the structure is in a None state</param>
        /// <returns>Tesult of invoking the None() operation if the optional 
        /// is in a None state, otherwise the bound Some(x) value is returned.</returns>
        [Pure]
        public Task<A> IfNoneUnsafe(Func<Task<A>> None) =>
            ifNoneUnsafeAsync<MOptionAsync<A>, OptionAsync<A>, A>(this, None);

        /// <summary>
        /// Returns the noneValue if the optional is in a None state, otherwise
        /// the bound Some(x) value is returned.
        /// </summary>
        /// <remarks>Will allow noneValue to be null</remarks>
        /// <param name="noneValue">Value to return if in a None state</param>
        /// <returns>noneValue if the optional is in a None state, otherwise
        /// the bound Some(x) value is returned</returns>
        [Pure]
        public Task<A> IfNoneUnsafe(A noneValue) =>
            ifNoneUnsafeAsync<MOptionAsync<A>, OptionAsync<A>, A>(this, noneValue);

        /// <summary>
        /// <para>
        /// Option types are like lists of 0 or 1 items, and therefore follow the 
        /// same rules when folding.
        /// </para><para>
        /// In the case of lists, 'Fold', when applied to a binary
        /// operator, a starting value(typically the left-identity of the operator),
        /// and a list, reduces the list using the binary operator, from left to
        /// right:
        /// </para>
        /// <para>
        /// Note that, since the head of the resulting expression is produced by
        /// an application of the operator to the first element of the list,
        /// 'Fold' can produce a terminating expression from an infinite list.
        /// </para>
        /// </summary>
        /// <typeparam name="S">Aggregate state type</typeparam>
        /// <param name="state">Initial state</param>
        /// <param name="folder">Folder function, applied if Option is in a Some state</param>
        /// <returns>The aggregate state</returns>
        [Pure]
        public Task<S> Fold<S>(S state, Func<S, A, S> folder) =>
            MOptionAsync<A>.Inst.FoldAsync(this, state, folder)(unit);

        /// <summary>
        /// <para>
        /// Option types are like lists of 0 or 1 items, and therefore follow the 
        /// same rules when folding.
        /// </para><para>
        /// In the case of lists, 'Fold', when applied to a binary
        /// operator, a starting value(typically the left-identity of the operator),
        /// and a list, reduces the list using the binary operator, from left to
        /// right:
        /// </para>
        /// <para>
        /// Note that, since the head of the resulting expression is produced by
        /// an application of the operator to the first element of the list,
        /// 'Fold' can produce a terminating expression from an infinite list.
        /// </para>
        /// </summary>
        /// <typeparam name="S">Aggregate state type</typeparam>
        /// <param name="state">Initial state</param>
        /// <param name="folder">Folder function, applied if Option is in a Some state</param>
        /// <returns>The aggregate state</returns>
        [Pure]
        public Task<S> Fold<S>(S state, Func<S, A, Task<S>> folder) =>
            MOptionAsync<A>.Inst.FoldAsync(this, state, folder)(unit);

        /// <summary>
        /// <para>
        /// Option types are like lists of 0 or 1 items, and therefore follow the 
        /// same rules when folding.
        /// </para><para>
        /// In the case of lists, 'Fold', when applied to a binary
        /// operator, a starting value(typically the left-identity of the operator),
        /// and a list, reduces the list using the binary operator, from left to
        /// right:
        /// </para>
        /// <para>
        /// Note that, since the head of the resulting expression is produced by
        /// an application of the operator to the first element of the list,
        /// 'Fold' can produce a terminating expression from an infinite list.
        /// </para>
        /// </summary>
        /// <typeparam name="S">Aggregate state type</typeparam>
        /// <param name="state">Initial state</param>
        /// <param name="folder">Folder function, applied if Option is in a Some state</param>
        /// <returns>The aggregate state</returns>
        [Pure]
        public Task<S> FoldBack<S>(S state, Func<S, A, S> folder) =>
            MOptionAsync<A>.Inst.FoldBackAsync(this, state, folder)(unit);

        /// <summary>
        /// <para>
        /// Option types are like lists of 0 or 1 items, and therefore follow the 
        /// same rules when folding.
        /// </para><para>
        /// In the case of lists, 'Fold', when applied to a binary
        /// operator, a starting value(typically the left-identity of the operator),
        /// and a list, reduces the list using the binary operator, from left to
        /// right:
        /// </para>
        /// <para>
        /// Note that, since the head of the resulting expression is produced by
        /// an application of the operator to the first element of the list,
        /// 'Fold' can produce a terminating expression from an infinite list.
        /// </para>
        /// </summary>
        /// <typeparam name="S">Aggregate state type</typeparam>
        /// <param name="state">Initial state</param>
        /// <param name="folder">Folder function, applied if Option is in a Some state</param>
        /// <returns>The aggregate state</returns>
        [Pure]
        public Task<S> FoldBack<S>(S state, Func<S, A, Task<S>> folder) =>
            MOptionAsync<A>.Inst.FoldBackAsync(this, state, folder)(unit);

        /// <summary>
        /// <para>
        /// Option types are like lists of 0 or 1 items, and therefore follow the 
        /// same rules when folding.
        /// </para><para>
        /// In the case of lists, 'Fold', when applied to a binary
        /// operator, a starting value(typically the left-identity of the operator),
        /// and a list, reduces the list using the binary operator, from left to
        /// right:
        /// </para>
        /// <para>
        /// Note that, since the head of the resulting expression is produced by
        /// an application of the operator to the first element of the list,
        /// 'Fold' can produce a terminating expression from an infinite list.
        /// </para>
        /// </summary>
        /// <typeparam name="S">Aggregate state type</typeparam>
        /// <param name="state">Initial state</param>
        /// <param name="Some">Folder function, applied if Option is in a Some state</param>
        /// <param name="None">Folder function, applied if Option is in a None state</param>
        /// <returns>The aggregate state</returns>
        [Pure]
        public Task<S> BiFold<S>(S state, Func<S, A, S> Some, Func<S, Unit, S> None) =>
            MOptionAsync<A>.Inst.BiFoldAsync(this, state, Some, None);

        /// <summary>
        /// <para>
        /// Option types are like lists of 0 or 1 items, and therefore follow the 
        /// same rules when folding.
        /// </para><para>
        /// In the case of lists, 'Fold', when applied to a binary
        /// operator, a starting value(typically the left-identity of the operator),
        /// and a list, reduces the list using the binary operator, from left to
        /// right:
        /// </para>
        /// <para>
        /// Note that, since the head of the resulting expression is produced by
        /// an application of the operator to the first element of the list,
        /// 'Fold' can produce a terminating expression from an infinite list.
        /// </para>
        /// </summary>
        /// <typeparam name="S">Aggregate state type</typeparam>
        /// <param name="state">Initial state</param>
        /// <param name="Some">Folder function, applied if Option is in a Some state</param>
        /// <param name="None">Folder function, applied if Option is in a None state</param>
        /// <returns>The aggregate state</returns>
        [Pure]
        public Task<S> BiFold<S>(S state, Func<S, A, Task<S>> Some, Func<S, Unit, S> None) =>
            MOptionAsync<A>.Inst.BiFoldAsync(this, state, Some, None);

        /// <summary>
        /// <para>
        /// Option types are like lists of 0 or 1 items, and therefore follow the 
        /// same rules when folding.
        /// </para><para>
        /// In the case of lists, 'Fold', when applied to a binary
        /// operator, a starting value(typically the left-identity of the operator),
        /// and a list, reduces the list using the binary operator, from left to
        /// right:
        /// </para>
        /// <para>
        /// Note that, since the head of the resulting expression is produced by
        /// an application of the operator to the first element of the list,
        /// 'Fold' can produce a terminating expression from an infinite list.
        /// </para>
        /// </summary>
        /// <typeparam name="S">Aggregate state type</typeparam>
        /// <param name="state">Initial state</param>
        /// <param name="Some">Folder function, applied if Option is in a Some state</param>
        /// <param name="None">Folder function, applied if Option is in a None state</param>
        /// <returns>The aggregate state</returns>
        [Pure]
        public Task<S> BiFold<S>(S state, Func<S, A, S> Some, Func<S, Unit, Task<S>> None) =>
            MOptionAsync<A>.Inst.BiFoldAsync(this, state, Some, None);

        /// <summary>
        /// <para>
        /// Option types are like lists of 0 or 1 items, and therefore follow the 
        /// same rules when folding.
        /// </para><para>
        /// In the case of lists, 'Fold', when applied to a binary
        /// operator, a starting value(typically the left-identity of the operator),
        /// and a list, reduces the list using the binary operator, from left to
        /// right:
        /// </para>
        /// <para>
        /// Note that, since the head of the resulting expression is produced by
        /// an application of the operator to the first element of the list,
        /// 'Fold' can produce a terminating expression from an infinite list.
        /// </para>
        /// </summary>
        /// <typeparam name="S">Aggregate state type</typeparam>
        /// <param name="state">Initial state</param>
        /// <param name="Some">Folder function, applied if Option is in a Some state</param>
        /// <param name="None">Folder function, applied if Option is in a None state</param>
        /// <returns>The aggregate state</returns>
        [Pure]
        public Task<S> BiFold<S>(S state, Func<S, A, Task<S>> Some, Func<S, Unit, Task<S>> None) =>
            MOptionAsync<A>.Inst.BiFoldAsync(this, state, Some, None);

        /// <summary>
        /// <para>
        /// Option types are like lists of 0 or 1 items, and therefore follow the 
        /// same rules when folding.
        /// </para><para>
        /// In the case of lists, 'Fold', when applied to a binary
        /// operator, a starting value(typically the left-identity of the operator),
        /// and a list, reduces the list using the binary operator, from left to
        /// right:
        /// </para>
        /// <para>
        /// Note that, since the head of the resulting expression is produced by
        /// an application of the operator to the first element of the list,
        /// 'Fold' can produce a terminating expression from an infinite list.
        /// </para>
        /// </summary>
        /// <typeparam name="S">Aggregate state type</typeparam>
        /// <param name="state">Initial state</param>
        /// <param name="Some">Folder function, applied if Option is in a Some state</param>
        /// <param name="None">Folder function, applied if Option is in a None state</param>
        /// <returns>The aggregate state</returns>
        [Pure]
        public Task<S> BiFold<S>(S state, Func<S, A, S> Some, Func<S, S> None) =>
            MOptionAsync<A>.Inst.BiFoldAsync(this, state, Some, (s, _) => None(s));

        /// <summary>
        /// <para>
        /// Option types are like lists of 0 or 1 items, and therefore follow the 
        /// same rules when folding.
        /// </para><para>
        /// In the case of lists, 'Fold', when applied to a binary
        /// operator, a starting value(typically the left-identity of the operator),
        /// and a list, reduces the list using the binary operator, from left to
        /// right:
        /// </para>
        /// <para>
        /// Note that, since the head of the resulting expression is produced by
        /// an application of the operator to the first element of the list,
        /// 'Fold' can produce a terminating expression from an infinite list.
        /// </para>
        /// </summary>
        /// <typeparam name="S">Aggregate state type</typeparam>
        /// <param name="state">Initial state</param>
        /// <param name="Some">Folder function, applied if Option is in a Some state</param>
        /// <param name="None">Folder function, applied if Option is in a None state</param>
        /// <returns>The aggregate state</returns>
        [Pure]
        public Task<S> BiFold<S>(S state, Func<S, A, Task<S>> Some, Func<S, S> None) =>
            MOptionAsync<A>.Inst.BiFoldAsync(this, state, Some, (s, _) => None(s));

        /// <summary>
        /// <para>
        /// Option types are like lists of 0 or 1 items, and therefore follow the 
        /// same rules when folding.
        /// </para><para>
        /// In the case of lists, 'Fold', when applied to a binary
        /// operator, a starting value(typically the left-identity of the operator),
        /// and a list, reduces the list using the binary operator, from left to
        /// right:
        /// </para>
        /// <para>
        /// Note that, since the head of the resulting expression is produced by
        /// an application of the operator to the first element of the list,
        /// 'Fold' can produce a terminating expression from an infinite list.
        /// </para>
        /// </summary>
        /// <typeparam name="S">Aggregate state type</typeparam>
        /// <param name="state">Initial state</param>
        /// <param name="Some">Folder function, applied if Option is in a Some state</param>
        /// <param name="None">Folder function, applied if Option is in a None state</param>
        /// <returns>The aggregate state</returns>
        [Pure]
        public Task<S> BiFold<S>(S state, Func<S, A, S> Some, Func<S, Task<S>> None) =>
            MOptionAsync<A>.Inst.BiFoldAsync(this, state, Some, (s, _) => None(s));

        /// <summary>
        /// <para>
        /// Option types are like lists of 0 or 1 items, and therefore follow the 
        /// same rules when folding.
        /// </para><para>
        /// In the case of lists, 'Fold', when applied to a binary
        /// operator, a starting value(typically the left-identity of the operator),
        /// and a list, reduces the list using the binary operator, from left to
        /// right:
        /// </para>
        /// <para>
        /// Note that, since the head of the resulting expression is produced by
        /// an application of the operator to the first element of the list,
        /// 'Fold' can produce a terminating expression from an infinite list.
        /// </para>
        /// </summary>
        /// <typeparam name="S">Aggregate state type</typeparam>
        /// <param name="state">Initial state</param>
        /// <param name="Some">Folder function, applied if Option is in a Some state</param>
        /// <param name="None">Folder function, applied if Option is in a None state</param>
        /// <returns>The aggregate state</returns>
        [Pure]
        public Task<S> BiFold<S>(S state, Func<S, A, Task<S>> Some, Func<S, Task<S>> None) =>
            MOptionAsync<A>.Inst.BiFoldAsync(this, state, Some, (s, _) => None(s));

        /// <summary>
        /// Projection from one value to another
        /// </summary>
        /// <typeparam name="B">Resulting functor value type</typeparam>
        /// <param name="Some">Projection function</param>
        /// <param name="None">Projection function</param>
        /// <returns>Mapped functor</returns>
        [Pure]
        public OptionAsync<B> BiMap<B>(Func<A, B> Some, Func<Unit, B> None) =>
            FOptionAsync<A, B>.Inst.BiMap(this, Some, None);

        /// <summary>
        /// Projection from one value to another
        /// </summary>
        /// <typeparam name="B">Resulting functor value type</typeparam>
        /// <param name="Some">Projection function</param>
        /// <param name="None">Projection function</param>
        /// <returns>Mapped functor</returns>
        [Pure]
        public OptionAsync<B> BiMap<B>(Func<A, B> Some, Func<B> None) =>
            FOptionAsync<A, B>.Inst.BiMap(this, Some, _ => None());

        /// <summary>
        /// <para>
        /// Return the number of bound values in this structure:
        /// </para>
        /// <para>
        ///     None = 0
        /// </para>
        /// <para>
        ///     Some = 1
        /// </para>
        /// </summary>
        /// <returns></returns>
        [Pure]
        public Task<int> Count() =>
            MOptionAsync<A>.Inst.CountAsync(this)(unit);

        /// <summary>
        /// Apply a predicate to the bound value.  If the Option is in a None state
        /// then True is returned (because the predicate applies for-all values).
        /// If the Option is in a Some state the value is the result of running 
        /// applying the bound value to the predicate supplied.        
        /// </summary>
        /// <param name="pred"></param>
        /// <returns>If the Option is in a None state then True is returned (because 
        /// the predicate applies for-all values).  If the Option is in a Some state
        /// the value is the result of running applying the bound value to the 
        /// predicate supplied.</returns>
        [Pure]
        public Task<bool> ForAll(Func<A, bool> pred) =>
            forallAsync<MOptionAsync<A>, OptionAsync<A>, A>(this, pred);

        /// <summary>
        /// Apply a predicate to the bound value.  If the Option is in a None state
        /// then True is returned if invoking None returns True.
        /// If the Option is in a Some state the value is the result of running 
        /// applying the bound value to the Some predicate supplied.        
        /// </summary>
        /// <param name="Some">Predicate to apply if in a Some state</param>
        /// <param name="None">Predicate to apply if in a None state</param>
        /// <returns>If the Option is in a None state then True is returned if 
        /// invoking None returns True. If the Option is in a Some state the value 
        /// is the result of running applying the bound value to the Some predicate 
        /// supplied.</returns>
        [Pure]
        public Task<bool> BiForAll(Func<A, bool> Some, Func<Unit, bool> None) =>
            biForAllAsync<MOptionAsync<A>, OptionAsync<A>, A, Unit>(this, Some, None);

        /// <summary>
        /// Apply a predicate to the bound value.  If the Option is in a None state
        /// then True is returned if invoking None returns True.
        /// If the Option is in a Some state the value is the result of running 
        /// applying the bound value to the Some predicate supplied.        
        /// </summary>
        /// <param name="Some">Predicate to apply if in a Some state</param>
        /// <param name="None">Predicate to apply if in a None state</param>
        /// <returns>If the Option is in a None state then True is returned if 
        /// invoking None returns True. If the Option is in a Some state the value 
        /// is the result of running applying the bound value to the Some predicate 
        /// supplied.</returns>
        [Pure]
        public Task<bool> BiForAll(Func<A, bool> Some, Func<bool> None) =>
            biForAllAsync<MOptionAsync<A>, OptionAsync<A>, A, Unit>(this, Some, _ => None());

        /// <summary>
        /// Apply a predicate to the bound value.  If the Option is in a None state
        /// then True is returned if invoking None returns True.
        /// If the Option is in a Some state the value is the result of running 
        /// applying the bound value to the Some predicate supplied.        
        /// </summary>
        /// <param name="pred"></param>
        /// <returns>If the Option is in a None state then True is returned if 
        /// invoking None returns True. If the Option is in a Some state the value 
        /// is the result of running applying the bound value to the Some predicate 
        /// supplied.</returns>
        [Pure]
        public Task<bool> Exists(Func<A, bool> pred) =>
            existsAsync<MOptionAsync<A>, OptionAsync<A>, A>(this, pred);

        /// <summary>
        /// Apply a predicate to the bound value.  If the Option is in a None state
        /// then True is returned if invoking None returns True.
        /// If the Option is in a Some state the value is the result of running 
        /// applying the bound value to the Some predicate supplied.        
        /// </summary>
        /// <param name="pred"></param>
        /// <returns>If the Option is in a None state then True is returned if 
        /// invoking None returns True. If the Option is in a Some state the value 
        /// is the result of running applying the bound value to the Some predicate 
        /// supplied.</returns>
        [Pure]
        public Task<bool> BiExists(Func<A, bool> Some, Func<Unit, bool> None) =>
            biExistsAsync<MOptionAsync<A>, OptionAsync<A>, A, Unit>(this, Some, None);

        /// <summary>
        /// Apply a predicate to the bound value.  If the Option is in a None state
        /// then True is returned if invoking None returns True.
        /// If the Option is in a Some state the value is the result of running 
        /// applying the bound value to the Some predicate supplied.        
        /// </summary>
        /// <param name="pred"></param>
        /// <returns>If the Option is in a None state then True is returned if 
        /// invoking None returns True. If the Option is in a Some state the value 
        /// is the result of running applying the bound value to the Some predicate 
        /// supplied.</returns>
        [Pure]
        public Task<bool> BiExists(Func<A, bool> Some, Func<bool> None) =>
            biExistsAsync<MOptionAsync<A>, OptionAsync<A>, A, Unit>(this, Some, _ => None());

        /// <summary>
        /// Invoke an action for the bound value (if in a Some state)
        /// </summary>
        /// <param name="Some">Action to invoke</param>
        [Pure]
        public Task<Unit> Iter(Action<A> Some) =>
            iterAsync<MOptionAsync<A>, OptionAsync<A>, A>(this, Some);

        /// <summary>
        /// Invoke an action depending on the state of the Option
        /// </summary>
        /// <param name="Some">Action to invoke if in a Some state</param>
        /// <param name="None">Action to invoke if in a None state</param>
        [Pure]
        public Task<Unit> BiIter(Action<A> Some, Action<Unit> None) =>
            biIterAsync<MOptionAsync<A>, OptionAsync<A>, A, Unit>(this, Some, None);

        /// <summary>
        /// Invoke an action depending on the state of the Option
        /// </summary>
        /// <param name="Some">Action to invoke if in a Some state</param>
        /// <param name="None">Action to invoke if in a None state</param>
        [Pure]
        public Task<Unit> BiIter(Action<A> Some, Action None) =>
            biIterAsync<MOptionAsync<A>, OptionAsync<A>, A, Unit>(this, Some, _ => None());

        /// <summary>
        /// Apply a predicate to the bound value (if in a Some state)
        /// </summary>
        /// <param name="pred">Predicate to apply</param>
        /// <returns>Some(x) if the Option is in a Some state and the predicate
        /// returns True.  None otherwise.</returns>
        [Pure]
        public OptionAsync<A> Filter(Func<A, bool> pred) =>
            filter<MOptionAsync<A>, OptionAsync<A>, A>(this, pred);

        /// <summary>
        /// Apply a predicate to the bound value (if in a Some state)
        /// </summary>
        /// <param name="pred">Predicate to apply</param>
        /// <returns>Some(x) if the Option is in a Some state and the predicate
        /// returns True.  None otherwise.</returns>
        [Pure]
        public OptionAsync<A> Where(Func<A, bool> pred) =>
            filter<MOptionAsync<A>, OptionAsync<A>, A>(this, pred);

        /// <summary>
        /// Monadic join
        /// </summary>
        [Pure]
        public OptionAsync<D> Join<B, C, D>(
            OptionAsync<B> inner,
            Func<A, C> outerKeyMap,
            Func<B, C> innerKeyMap,
            Func<A, B, D> project) =>
            join<EqDefault<C>, MOptionAsync<A>, MOptionAsync<B>, MOptionAsync<D>, OptionAsync<A>, OptionAsync<B>, OptionAsync<D>, A, B, C, D>(
                this, inner, outerKeyMap, innerKeyMap, project
                );

        /// <summary>
        /// Partial application map
        /// </summary>
        /// <remarks>TODO: Better documentation of this function</remarks>
        [Pure]
        public OptionAsync<Func<B, C>> ParMap<B, C>(Func<A, B, C> func) =>
            Map(curry(func));

        /// <summary>
        /// Partial application map
        /// </summary>
        /// <remarks>TODO: Better documentation of this function</remarks>
        [Pure]
        public OptionAsync<Func<B, Func<C, D>>> ParMap<B, C, D>(Func<A, B, C, D> func) =>
            Map(curry(func));
    }
}
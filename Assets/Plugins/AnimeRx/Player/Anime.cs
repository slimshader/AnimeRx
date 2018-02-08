﻿using System;
using System.Collections;
using System.Threading;
using UniRx;

namespace AnimeRx
{
    public static partial class Anime
    {
        private static IScheduler defaultScheduler = new TimeScheduler();

        public static IScheduler DefaultScheduler
        {
            get { return defaultScheduler; }
            set { defaultScheduler = value; }
        }

        public static IObservable<float> Play(IAnimator animator)
        {
            return Play(animator, DefaultScheduler);
        }

        public static IObservable<float> Play(IAnimator animator, IScheduler scheduler)
        {
            return PlayInternal(animator, 1.0f, scheduler);
        }

        private static IObservable<float> PlayInternal(IAnimator animator, float distance, IScheduler scheduler)
        {
            return Observable
                .Defer(() => Observable.Return(scheduler.Now))
                .ContinueWith(start =>
                    Observable.FromMicroCoroutine<float>((observer, token) => AnimationCoroutine(animator, start, distance, scheduler, observer, token))
                );
        }

        private static IObservable<Unit> DelayInternal(float duration, IScheduler scheduler)
        {
            return Observable
                .Defer(() => Observable.Return(scheduler.Now))
                .ContinueWith(start =>
                    Observable.FromMicroCoroutine<Unit>((observer, token) => DelayCoroutine(start, duration, scheduler, observer, token))
                );
        }

        private static IEnumerator AnimationCoroutine(IAnimator animator, float start, float distance, IScheduler scheduler, IObserver<float> observer, CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    observer.OnCompleted();
                    yield break;
                }

                var now = scheduler.Now - start;
                if (animator.CalcFinishTime(distance) < now)
                {
                    break;
                }

                observer.OnNext(animator.CalcPosition(now, distance));
                yield return null;
            }

            observer.OnNext(animator.CalcPosition(animator.CalcFinishTime(distance), distance));
            observer.OnCompleted();
        }

        private static IEnumerator DelayCoroutine(float start, float duration, IScheduler scheduler, IObserver<Unit> observer, CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    observer.OnCompleted();
                    yield break;
                }

                var now = scheduler.Now - start;
                if (duration < now)
                {
                    break;
                }

                yield return null;
            }

            observer.OnCompleted();
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace VisionFramework.Runtime
{
    /// <summary>
    /// 图像缓冲队列——生产者-消费者模式解耦采集和处理。
    /// 借鉴 OpenIVS 的 BlockingCollection 设计。
    /// 采集线程入队，处理线程出队，互不阻塞。
    /// </summary>
    public class ImageBufferQueue<T> : IDisposable
    {
        private readonly BlockingCollection<T> _queue;
        private readonly int _capacity;

        public int Count => _queue.Count;
        public int Capacity => _capacity;
        public bool IsCompleted => _queue.IsCompleted;

        public ImageBufferQueue(int capacity = 3)
        {
            _capacity = capacity;
            _queue = new BlockingCollection<T>(capacity);
        }

        /// <summary>入队（采集线程调用）。队列满时丢弃最旧的图像。</summary>
        public void Enqueue(T item)
        {
            while (!_queue.TryAdd(item, 0))
            {
                if (_queue.TryTake(out _)) continue;
                break;
            }
        }

        /// <summary>出队（处理线程调用）。阻塞等待。</summary>
        public T Dequeue(CancellationToken token = default)
        {
            return _queue.Take(token);
        }

        /// <summary>尝试出队（不阻塞）。</summary>
        public bool TryDequeue(out T item, int timeoutMs = 0)
        {
            return _queue.TryTake(out item, timeoutMs);
        }

        public void CompleteAdding() { _queue.CompleteAdding(); }
        public void Dispose() { _queue?.Dispose(); }
    }
}
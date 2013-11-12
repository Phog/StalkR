// NativeFaceDetector.cpp
#include "pch.h"
#include "NativeFaceDetector.h"

#include <cassert>
#include <algorithm>

using namespace NativeFaceDetector;
using namespace Platform;

Detector::Detector(int width, int height)
	: m_size(width, height), m_grayImage(Size(640, 480)), m_squaredImage(Size(640, 480))
{
}

Detector::~Detector()
{
	if (m_currentFeature)
	{
		delete m_currentFeature;
		m_currentFeature = 0;
	}
}

void Detector::makeFeature(float threshold, float leftVal, int leftNode, float rightVal,
						   int rightNode, int width, int height)
{
	assert(!m_currentFeature);
	m_currentFeature = new Feature(threshold, leftVal, leftNode, rightVal, rightNode, Size(width, height));
}

void Detector::addFeature()
{
	m_currentTree.addFeature(*m_currentFeature);
	delete m_currentFeature;
	m_currentFeature = 0;
}

concurrency::task<std::vector<Rectangle^> > Detector::rectsForScaleParallel(int i0, int width, int height, int size, int xStep, int yStep)
{
	return concurrency::create_task([i0, width, height, size, xStep, yStep, this]
	{
		std::vector<Rectangle^> rects;
		for (int i = i0; i < height; i += yStep)
		{
			for (int j = 0; j < width; j += xStep)
			{
				bool pass = true;
				for (std::vector<Stage>::const_iterator it = m_stages.cbegin(); it < m_stages.cend(); ++it)
				{
					if (!it->pass(m_grayImage, m_squaredImage, i, j))
					{
						pass = false;
						break;
					}
				}
				
				if (pass)
					rects.push_back(ref new Rectangle(j, i, size, size));
			}
		}
		return rects;
	});
}

void Detector::getFaces(Windows::Foundation::Collections::IVector<Rectangle^> ^outRects, 
						const Platform::Array<int, 1> ^imageData, int width, int height,
						float baseScale, float scaleInc, float increment, int minNeighbors)
{
	std::vector<Rectangle^> rects;
	float maxScale = min((float)width / m_size.width, (float)height / m_size.height);

	Size imageSize(width, height);
	m_grayImage.resize(imageSize);
	m_squaredImage.resize(imageSize);

	const int *imgData = imageData->Data;
	for (int i = 0; i < height; i++)
	{
		int row        = 0;
		int rowSquared = 0;
		for (int j = 0; j < width; j++)
		{
			int c = imgData[i * width + j];
			int r = (c & 0x00ff0000) >> 16;
			int g = (c & 0x0000ff00) >> 8;
			int b = c & 0x000000ff;
			int v = (30 * r + 59 * g + 11 * b) / 100;

			row        += v;
			rowSquared += v * v;
			
			m_grayImage(i, j)    = (i > 0 ? m_grayImage(i - 1, j) : 0) + row;
			m_squaredImage(i, j) = (i > 0 ? m_squaredImage(i - 1, j) : 0) + rowSquared;
		}
	}

	for (float scale = baseScale; scale < maxScale; scale *= scaleInc)
	{
		for (std::vector<Stage>::iterator it = m_stages.begin(); it < m_stages.end(); ++it)
			it->setScale(scale);
			
		int step = (int)(scale * m_size.width * increment);
		int size = (int)(scale * m_size.width);

		auto aThread = rectsForScaleParallel(0   , width - size, height - size, size, step, 2 * step);
		auto bThread = rectsForScaleParallel(step, width - size, height - size, size, step, 2 * step);

		std::vector<Rectangle^> aVec = aThread.get();
		for (auto it = aVec.begin(); it != aVec.end(); ++it)
			rects.push_back(*it);

		std::vector<Rectangle^> bVec = bThread.get();
		for (auto it = bVec.begin(); it != bVec.end(); ++it)
			rects.push_back(*it);
	}

	merge(outRects, rects, minNeighbors);
}

void Detector::merge(Windows::Foundation::Collections::IVector<Rectangle^> ^outRects,
			         const std::vector<Rectangle^> &inRects, int minNeighbors)
{
	std::vector<int> ret(inRects.size());
    
    size_t numClasses = 0;
    for (size_t i = 0; i < inRects.size(); i++)
    {
        bool found = false;
        for (size_t j = 0; j < i; j++)
            if (inRects[j]->approximatelyEqual(inRects[i]))
            {
                found = true;
                ret[i] = ret[j];
            }

        if (!found)
            ret[i] = numClasses++;
    }

	std::vector<size_t> neighbours(numClasses);
	std::vector<Rectangle^> rects(numClasses);

    for (size_t i = 0; i < numClasses; i++)
    {
        neighbours[i] = 0;
        rects[i] = ref new Rectangle(0, 0, 0, 0);
    }

    for (size_t i = 0; i < inRects.size(); i++)
    {
        neighbours[ret[i]]++;

		double x      = rects[ret[i]]->x() + inRects[i]->x();
		double y      = rects[ret[i]]->y() + inRects[i]->y();
		double width  = rects[ret[i]]->width() + inRects[i]->width();
		double height = rects[ret[i]]->height() + inRects[i]->height();

		rects[ret[i]]->x(x);
		rects[ret[i]]->y(y);
		rects[ret[i]]->width(width);
		rects[ret[i]]->height(height);
    }

    for (size_t i = 0; i < numClasses; i++)
    {
        size_t n = neighbours[i];
        if (n >= (size_t)minNeighbors)
        {
			double x      = (rects[i]->x() * 2 + n) / (2 * n);
            double y      = (rects[i]->y() * 2 + n) / (2 * n);
            double width  = (rects[i]->width() * 2 + n) / (2 * n);
            double height = (rects[i]->height() * 2 + n) / (2 * n);

			outRects->Append(ref new Rectangle(x, y, width, height));
        }
    }
}
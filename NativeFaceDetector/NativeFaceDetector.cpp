// NativeFaceDetector.cpp
#include "pch.h"
#include "NativeFaceDetector.h"

#include <cassert>
#include <algorithm>

using namespace NativeFaceDetector;
using namespace Platform;

Detector::Detector(int width, int height)
	: m_size(width, height), m_canny(Size(640, 480)), m_image(Size(640, 480)), m_grayImage(Size(640, 480)), m_squaredImage(Size(640, 480))
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

void Detector::getFaces(Windows::Foundation::Collections::IVector<Rectangle^> ^outRects, 
						const Platform::Array<int, 1> ^imageData, int width, int height,
						float baseScale, float scaleInc, float increment, int minNeighbors,
						bool doCannyPruning)
{
	std::vector<Rectangle^> rects;
	float maxScale = std::min((float)width / m_size.width, (float)height / m_size.height);

	Size imageSize(width, height);
	m_image.resize(imageSize);
	m_grayImage.resize(imageSize);
	m_squaredImage.resize(imageSize);

	for (int i = 0; i < width; i++)
	{
		int col = 0;
		int colSquared = 0;
		for (int j = 0; j < height; j++)
		{
			int c     = imageData[j * width + i];
			int red   = (c & 0x00ff0000) >> 16;
			int green = (c & 0x0000ff00) >> 8;
			int blue  = c & 0x000000ff;
			int value = (30 * red + 59 * green + 11 * blue) / 100;

			col        += value;
			colSquared += value * value;

			m_image(i, j)        = value;
			m_grayImage(i, j)    = (i > 0 ? m_grayImage(i - 1, j) : 0) + col;
			m_squaredImage(i, j) = (i > 0 ? m_squaredImage(i - 1, j) : 0) + colSquared;
		}
	}

	if (doCannyPruning)
		makeCannyIntegral(m_image);

	for (float scale = baseScale; scale < maxScale; scale *= scaleInc)
	{
		for (std::vector<Stage>::iterator it = m_stages.begin(); it < m_stages.end(); ++it)
			it->setScale(scale);
			
		int step = (int)(scale * m_size.width * increment);
		int size = (int)(scale * m_size.width);
		for (int i = 0; i < width - size; i += step)
		{
			for (int j = 0; j < height - size; j += step)
			{
				if (doCannyPruning)
				{
					int edgeDensity = m_canny(i + size, j + size) + m_canny(i, j) - m_canny(i, j + size) - m_canny(i + size, j);
					int d = edgeDensity / (size * size);
					if (d < 20 || d > 100)
						continue;
				} 

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
					rects.push_back(ref new Rectangle(i, j, size, size));
			}
		}
	}

	merge(outRects, rects, minNeighbors);
}

void Detector::makeCannyIntegral(const Matrix &grayImage)
{
	/* Convolution of the image by a gaussian filter to reduce noise.*/
	Size size = grayImage.size();
	m_canny.resize(size);

	for (int i = 2; i < size.width - 2; i++)
		for (int j = 2; j < size.height - 2; j++)
		{
			int sum = 0;
			sum += 2 * grayImage(i-2, j-2);
			sum += 4 * grayImage(i-2, j-1);
			sum += 5 * grayImage(i-2, j+0);
			sum += 4 * grayImage(i-2, j+1);
			sum += 2 * grayImage(i-2, j+2);
			sum += 4 * grayImage(i-1, j-2);
			sum += 9 * grayImage(i-1, j-1);
			sum += 12 * grayImage(i-1, j+0);
			sum += 9 * grayImage(i-1, j+1);
			sum += 4 * grayImage(i-1, j+2);
			sum += 5 * grayImage(i+0, j-2);
			sum += 12 * grayImage(i+0, j-1);
			sum += 15 * grayImage(i+0, j+0);
			sum += 12 * grayImage(i+0, j+1);
			sum += 5 * grayImage(i+0, j+2);
			sum += 4 * grayImage(i+1, j-2);
			sum += 9 * grayImage(i+1, j-1);
			sum += 12 * grayImage(i+1, j+0);
			sum += 9 * grayImage(i+1, j+1);
			sum += 4 * grayImage(i+1, j+2);
			sum += 2 * grayImage(i+2, j-2);
			sum += 4 * grayImage(i+2, j-1);
			sum += 5 * grayImage(i+2, j+0);
			sum += 4 * grayImage(i+2, j+1);
			sum += 2 * grayImage(i+2, j+2);

			m_canny(i, j) = sum / 159;
		}

	// Compute the discrete gradient of the image.
	Matrix grad(size);
	for (int i = 1; i < size.width - 1; i++)
		for (int j = 1; j < size.height - 1; j++)
		{
			int grad_x = -m_canny(i - 1, j - 1) + m_canny(i + 1, j - 1) - 2 * m_canny(i - 1, j) + 2 * m_canny(i + 1, j) - m_canny(i - 1, j + 1) + m_canny(i + 1, j + 1);
			int grad_y =  m_canny(i - 1, j - 1) + 2 * m_canny(i, j - 1) + m_canny(i + 1, j - 1) - m_canny(i - 1, j + 1) - 2 * m_canny(i, j + 1) - m_canny(i + 1, j + 1);
			grad(i, j) = abs(grad_x) + abs(grad_y);
		}

	for (int i = 0; i < size.width; i++)
	{
		int col=0;
		for (int j = 0; j < size.height; j++)
		{
			int value = grad(i, j);
			m_canny(i, j) = (i > 0 ? m_canny(i - 1, j) : 0) + col + value;
			col += value;
		}
	}
}

void Detector::merge(Windows::Foundation::Collections::IVector<Rectangle^> ^outRects,
			         const std::vector<Rectangle^> &inRects, int minNeighbors)
{
	std::vector<int> ret(inRects.size());
    
    int numClasses = 0;
    for (int i = 0; i < inRects.size(); i++)
    {
        bool found = false;
        for (int j = 0; j < i; j++)
            if (inRects[j]->approximatelyEqual(inRects[i]))
            {
                found = true;
                ret[i] = ret[j];
            }

        if (!found)
            ret[i] = numClasses++;
    }

	std::vector<int> neighbours(numClasses);
	std::vector<Rectangle^> rects(numClasses);

    for (int i = 0; i < numClasses; i++)
    {
        neighbours[i] = 0;
        rects[i] = ref new Rectangle(0, 0, 0, 0);
    }

    for (int i = 0; i < inRects.size(); i++)
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

    for (int i = 0; i < numClasses; i++)
    {
        int n = neighbours[i];
        if (n >= minNeighbors)
        {
			double x      = (rects[i]->x() * 2 + n) / (2 * n);
            double y      = (rects[i]->y() * 2 + n) / (2 * n);
            double width  = (rects[i]->width() * 2 + n) / (2 * n);
            double height = (rects[i]->height() * 2 + n) / (2 * n);

			outRects->Append(ref new Rectangle(x, y, width, height));
        }
    }
}
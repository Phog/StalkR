#pragma once

#include "Stage.h"
#include "Matrix.h"
#include "Rectangle.h"
#include "Structures.h"

#include <vector>
#include <ppltasks.h>

namespace NativeFaceDetector
{

    public ref class Detector sealed
    {
		std::vector<Stage> m_stages;
		Size m_size;
		Matrix m_grayImage, m_squaredImage;

		Tree m_currentTree;
		Feature *m_currentFeature;

		void merge(Windows::Foundation::Collections::IVector<Rectangle^> ^outRects,
			       const std::vector<Rectangle^> &inRects, int minNeighbors);

		concurrency::task<std::vector<Rectangle^> > rectsForScaleParallel(int i, int width, int height, int size, int xStep, int yStep);

    public:
        Detector(int width, int height);
		virtual ~Detector();

		void addStage(float threshold) { m_stages.push_back(Stage(threshold)); }
		void addTree() { m_stages.back().addTree(m_currentTree); m_currentTree = Tree();  }
		
		void makeFeature(float threshold, float leftVal, int leftNode, float rightVal, int rightNode, int width, int height);
		void addFeature();
		void addRect(int x1, int x2, int y1, int y2, float weight) { m_currentFeature->add(RectFeature(x1, x2, y1, y2, weight)); }

		void getFaces(Windows::Foundation::Collections::IVector<Rectangle^> ^rects, 
			          const Platform::Array<int, 1> ^imageData, int width, int height,
					  float baseScale, float scaleInc, float increment, int minNeighbors);
    };
}